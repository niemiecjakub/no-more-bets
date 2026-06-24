using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Betting.GetBetEvents;
using NoMoreBets.Application.Betting.UpdateMatches;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

/// <summary>
/// Keeps listed bookmaker fixtures in sync with the app and captures periodic odds snapshots for upcoming games.
/// </summary>
public sealed class BookmakerListingSyncJobService(
  IMediator mediator,
  AppDbContext db,
  ILogger<BookmakerListingSyncJobService> logger)
{
  /// <summary>
  /// Daily job: loads leagues and schedules <see cref="GetBetclicGamesForLeague"/> per league with a random delay (0-900s).
  /// </summary>
  [AutomaticRetry(Attempts = 1)]
  public async Task GetBetclicGames()
  {
    var leagues = await db.League
      .Where(l => l.SoccerdataId > 0)
      .Select(l => new { l.Id, l.Name })
      .ToListAsync();
    if (leagues.Count == 0)
    {
      logger.LogWarning(
        "Job {JobName} found no leagues in database. Skipping Betclic refresh.",
        nameof(GetBetclicGames));
      return;
    }

    logger.LogInformation(
      "Starting job {JobName} to schedule upcoming Betclic game refresh for {LeagueCount} leagues",
      nameof(GetBetclicGames),
      leagues.Count);

    foreach (var league in leagues)
    {
      var delay = TimeSpan.FromSeconds(Random.Shared.Next(0, 900));
      BackgroundJob.Schedule<BookmakerListingSyncJobService>(
        js => js.GetBetclicGamesForLeague(league.Id),
        delay);
      logger.LogInformation(
        "Job {JobName} scheduled Betclic refresh for league {LeagueId} ({LeagueName}) after {DelaySeconds}s",
        nameof(GetBetclicGames),
        league.Id,
        league.Name,
        delay.TotalSeconds);
    }

    logger.LogInformation(
      "Job {JobName} scheduled {LeagueCount} per-league Betclic refresh jobs (random delay 0-900s each)",
      nameof(GetBetclicGames),
      leagues.Count);
  }

  [AutomaticRetry(Attempts = 1)]
  public async Task GetBetclicGamesForLeague(int leagueId)
  {
    var leagueName = await db.League
      .Where(l => l.Id == leagueId)
      .Select(l => l.Name)
      .FirstOrDefaultAsync();

    var upcomingGames = await mediator.Send(new UpdateMatchesCommand(leagueId));
    logger.LogInformation(
      "Job {JobName} received {GameCount} upcoming Betclic games for league {LeagueId} ({LeagueName})",
      nameof(GetBetclicGamesForLeague),
      upcomingGames.Count,
      leagueId,
      leagueName ?? "(unknown)");
  }

  public async Task ScheduleBettingOddsJob()
  {
    logger.LogInformation(
      "Starting job {JobName} to schedule betting odds for upcoming matches",
      nameof(ScheduleBettingOddsJob));

    var utcNow = DateTime.UtcNow;
    var kickoffWithinTenDaysEnd = utcNow.AddDays(10);

    var upcommingGames = await db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming
        && m.BetclicUrl != null
        && m.MatchDate > utcNow
        && m.MatchDate <= kickoffWithinTenDaysEnd)
      .Select(m => new { m.BetclicUrl })
      .ToListAsync();

    logger.LogInformation(
      "Job {JobName} found {MatchCount} upcoming matches with Betclic URL to schedule",
      nameof(ScheduleBettingOddsJob),
      upcommingGames.Count);

    foreach (var match in upcommingGames)
    {
      var delay = TimeSpan.FromSeconds(Random.Shared.Next(0, 300));
      BackgroundJob.Schedule<BookmakerListingSyncJobService>(
          js => js.GetBettingOdds(match.BetclicUrl!),
          delay);
    }

    logger.LogInformation(
      "Job {JobName} scheduled {JobCount} betting odds jobs",
      nameof(ScheduleBettingOddsJob),
      upcommingGames.Count);
  }

  [AutomaticRetry(Attempts = 0)]
  public async Task GetBettingOdds(string gameUrl)
  {
    logger.LogInformation(
      "Starting job {JobName} to get betting odds for {GameUrl}",
      nameof(GetBettingOdds),
      gameUrl);

    if (string.IsNullOrWhiteSpace(gameUrl))
    {
      logger.LogWarning(
        "Job {JobName} received empty or whitespace Betclic game URL",
        nameof(GetBettingOdds));
      return;
    }

    try
    {
      var events = await mediator.Send(new GetBetclicMatchEventsQuery(gameUrl, Expand: true));
      if (events is null || events.Count == 0)
      {
        logger.LogWarning(
          "Job {JobName} received no events for Betclic game at {GameUrl}",
          nameof(GetBettingOdds),
          gameUrl);
        return;
      }

      var match = await db.Match
        .Include(m => m.HomeClub)
        .Include(m => m.AwayClub)
        .Where(m => m.BetclicUrl == gameUrl)
        .SingleOrDefaultAsync();

      if (match is null)
      {
        logger.LogWarning(
          "Job {JobName} found no match in DB for Betclic game at {GameUrl}",
          nameof(GetBettingOdds),
          gameUrl);
        return;
      }

      if (match.MatchStatus == MatchStatus.Finished)
      {
        RecurringJob.RemoveIfExists(GetBettingOddsJobId(match.Id));
        logger.LogInformation(
          "Job {JobName} removed recurring job for finished match {MatchId} at {GameUrl}",
          nameof(GetBettingOdds),
          match.Id,
          gameUrl);
        return;
      }

      var utcNow = DateTime.UtcNow;
      if (match.MatchDate <= utcNow || match.MatchDate > utcNow.AddDays(10))
      {
        logger.LogInformation(
          "Job {JobName} skipped match {MatchId} at {GameUrl} because kickoff {KickoffUtc} is outside the 10-day scraping window",
          nameof(GetBettingOdds),
          match.Id,
          gameUrl,
          match.MatchDate);
        return;
      }

      var snapshot = new BettingOddsSnapshot
      {
        MatchId = match.Id,
        SnapshotTime = DateTime.UtcNow
      };

      foreach (var ev in events)
      {
        var eventType = BookmakerEventTypeMapper.Map(ev.Title);
        if (eventType is null)
        {
          continue;
        }

        var rows = BookmakerEventOptionMapper.MapToRows(
                   ev.Options,
                   eventType.Value,
                   match);
        foreach (var row in rows)
        {
          if (row.EventOptionId == null)
          {
            continue;
          }
          snapshot.Rows.Add(row);
        }
      }

      if (snapshot.Rows.Count == 0)
      {
        logger.LogWarning(
          "Job {JobName} built an empty betting odds snapshot for match {MatchId} at {GameUrl}",
          nameof(GetBettingOdds),
          match.Id,
          gameUrl);
        return;
      }

      snapshot.EnsureCompleteBettingEventOptionsCoverage();

      db.BettingOddsSnapshot.Add(snapshot);
      await db.SaveChangesAsync();

      logger.LogInformation(
        "Job {JobName} saved betting odds snapshot with {RowCount} rows for match {MatchId} at {GameUrl}",
        nameof(GetBettingOdds),
        snapshot.Rows.Count,
        match.Id,
        gameUrl);
    }
    catch (Exception ex)
    {
      logger.LogError(
        ex,
        "Job {JobName} failed while processing betting odds for {GameUrl}",
        nameof(GetBettingOdds),
        gameUrl);
      throw;
    }
  }

  private static string GetBettingOddsJobId(int matchId) => $"betting-odds-{matchId}";
}
