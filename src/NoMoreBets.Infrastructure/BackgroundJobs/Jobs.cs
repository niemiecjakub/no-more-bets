using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Betting.GetBetEvents;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Features.Betclic.RefreshBetclicGames;
using NoMoreBets.Features.Fotmob.RefreshLeagueTableSnapshot;
using NoMoreBets.Features.Rotowire.GetRotowireLineups;
using NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;
using NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;
using NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;
using NoMoreBets.Infrastructure.Persistence;
using System.Text.Json;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public class JobService(IMediator mediator, AppDbContext db, ILogger<JobService> logger)
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  [AutomaticRetry(Attempts = 3)]
  public async Task GetUpcommingSoccerdataMatches(int soccerdataLeagueId)
  {
    logger.LogInformation(
      "Starting job {JobName} for Soccerdata league {SoccerdataLeagueId}",
      nameof(GetUpcommingSoccerdataMatches),
      soccerdataLeagueId);

    var upcommingMatches = await mediator.Send(new UpdateUpcommingMatchesCommand(soccerdataLeagueId));
    logger.LogInformation(
      "Job {JobName} fetched {MatchCount} upcoming matches for Soccerdata league {SoccerdataLeagueId}",
      nameof(GetUpcommingSoccerdataMatches),
      upcommingMatches.Count,
      soccerdataLeagueId);

    var enqueuedJobs = 0;

    foreach (var match in upcommingMatches)
    {
      if (!match.SoccerdataId.HasValue)
      {
        logger.LogWarning(
          "Job {JobName} skipping match {MatchId} because SoccerdataId is missing",
          nameof(GetUpcommingSoccerdataMatches),
          match.Id);
        continue;
      }
      BackgroundJob.Enqueue(() => GetUpcommingSoccerdataMatchePreview(match.SoccerdataId.Value));
      BackgroundJob.Enqueue(() => RefreshHead2HeadStatistics(match.HomeClubId, match.AwayClubId));
      enqueuedJobs += 2;
    }

    logger.LogInformation(
      "Job {JobName} enqueued {JobCount} Hangfire jobs for Soccerdata upcoming matches for league {SoccerdataLeagueId}",
      nameof(GetUpcommingSoccerdataMatches),
      enqueuedJobs,
      soccerdataLeagueId);
  }

  [AutomaticRetry(Attempts = 3)]
  public async Task GetUpcommingSoccerdataMatchePreview(int soccerdataMatchId)
  {
    logger.LogInformation(
      "Starting job {JobName} for Soccerdata match {SoccerdataMatchId}",
      nameof(GetUpcommingSoccerdataMatchePreview),
      soccerdataMatchId);

    await mediator.Send(new UpdateUpcommingMatchPreviewCommand(soccerdataMatchId));

    logger.LogInformation(
      "Completed job {JobName} for Soccerdata match {SoccerdataMatchId}",
      nameof(GetUpcommingSoccerdataMatchePreview),
      soccerdataMatchId);
  }

  [AutomaticRetry(Attempts = 3)]
  public async Task RefreshHead2HeadStatistics(int homeClubId, int awayClubId)
  {
    logger.LogInformation(
      "Starting job {JobName} for clubs {HomeClubId} vs {AwayClubId}",
      nameof(RefreshHead2HeadStatistics),
      homeClubId,
      awayClubId);

    var clubSoccerdataIds = await db.Club
      .Where(c => c.Id == homeClubId || c.Id == awayClubId)
      .Select(c => new { c.Id, c.SoccerdataId })
      .ToListAsync();

    if (clubSoccerdataIds.Count != 2)
    {
      logger.LogWarning(
        "Job {JobName} could not find both clubs in DB. Expected 2, found {Count}. HomeClubId={HomeClubId}, AwayClubId={AwayClubId}",
        nameof(RefreshHead2HeadStatistics),
        clubSoccerdataIds.Count,
        homeClubId,
        awayClubId);
      return;
    }

    var homeClub = clubSoccerdataIds.FirstOrDefault(c => c.Id == homeClubId);
    var awayClub = clubSoccerdataIds.FirstOrDefault(c => c.Id == awayClubId);
    if (homeClub?.SoccerdataId is not { } homeSoccerdataId || awayClub?.SoccerdataId is not { } awaySoccerdataId)
    {
      logger.LogWarning(
        "Job {JobName} cannot refresh head-to-head: missing SoccerdataId for one or both clubs. HomeClubId={HomeClubId}, HomeSoccerdataId={HomeSoccerdataId}, AwayClubId={AwayClubId}, AwaySoccerdataId={AwaySoccerdataId}",
        nameof(RefreshHead2HeadStatistics),
        homeClubId,
        homeClub?.SoccerdataId,
        awayClubId,
        awayClub?.SoccerdataId);
      return;
    }

    var (homeClubSoccerdataId, awayClubSoccerdataId) = (homeSoccerdataId, awaySoccerdataId);
    var h2h = await db.Head2Head
        .ForClubs(homeClubSoccerdataId, awayClubSoccerdataId)
        .FirstOrDefaultAsync();

    bool shouldUpdate = h2h == null;

    if (h2h != null)
    {
      var lastFinishedGameDate = await db.Match
          .ForClubs(homeClubSoccerdataId, awayClubSoccerdataId)
          .Where(m => m.MatchStatus == Domain.Enums.MatchStatus.Finished)
          .OrderByDescending(m => m.MatchDate)
          .Select(m => (DateTime?)m.MatchDate)
          .FirstOrDefaultAsync();

      shouldUpdate = lastFinishedGameDate > h2h.UpdatedAt;
    }

    if (shouldUpdate)
    {
      BackgroundJob.Enqueue<JobService>(js => js.RefreshHead2HeadData(homeClubSoccerdataId, awayClubSoccerdataId));
      logger.LogInformation(
        "Job {JobName} enqueued head-to-head refresh for clubs {HomeClubSoccerdataId} vs {AwayClubSoccerdataId}",
        nameof(RefreshHead2HeadStatistics),
        homeClubSoccerdataId,
        awayClubSoccerdataId);
    }
    else
    {
      logger.LogInformation(
        "Job {JobName} skipped head-to-head refresh because data is up to date for clubs {HomeClubSoccerdataId} vs {AwayClubSoccerdataId}",
        nameof(RefreshHead2HeadStatistics),
        homeClubSoccerdataId,
        awayClubSoccerdataId);
    }
  }

  [AutomaticRetry(Attempts = 3)]
  public async Task RefreshHead2HeadData(int homeClubSoccerdataId, int awayClubSoccerdataId)
  {
    await mediator.Send(new UpdateHeadToHeadCommand(homeClubSoccerdataId, awayClubSoccerdataId));
  }

  [AutomaticRetry(Attempts = 3)]
  public async Task GetLineups()
  {
    logger.LogInformation(
      "Starting job {JobName} to refresh Rotowire lineups",
      nameof(GetLineups));

    await mediator.Send(new UpdateLineupsCommand());

    logger.LogInformation(
      "Completed job {JobName} to refresh Rotowire lineups",
      nameof(GetLineups));
  }

  [AutomaticRetry(Attempts = 3)]
  public async Task GetLeagueTable()
  {
    logger.LogInformation(
      "Starting job {JobName} to refresh league table for Premier League",
      nameof(GetLeagueTable));

    var premierLeagueId = await db.League
      .Where(l => l.Name == "Premier League")
      .Select(l => l.Id)
      .FirstOrDefaultAsync();

    if (premierLeagueId == 0)
    {
      logger.LogWarning(
        "Job {JobName} could not find Premier League in database. Skipping refresh.",
        nameof(GetLeagueTable));
      return;
    }

    await mediator.Send(new UpdateTableCommand(premierLeagueId));

    logger.LogInformation(
      "Completed job {JobName} for league {LeagueId}",
      nameof(GetLeagueTable),
      premierLeagueId);
  }

  [AutomaticRetry(Attempts = 1)]
  public async Task CloseStartingSoonMatches()
  {
    var now = DateTime.UtcNow;
    var cutoff = now.AddHours(2);

    var matchesToClose = await db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming && m.MatchDate <= cutoff)
      .ToListAsync();

    logger.LogInformation(
      "Job {JobName} found {MatchCount} upcoming matches starting before cutoff {Cutoff}",
      nameof(CloseStartingSoonMatches),
      matchesToClose.Count,
      cutoff);

    if (matchesToClose.Count == 0)
    {
      logger.LogInformation(
        "Job {JobName} found no matches to close",
        nameof(CloseStartingSoonMatches));
      return;
    }

    foreach (var match in matchesToClose)
    {
      match.MatchStatus = MatchStatus.Finished;
    }

    await db.SaveChangesAsync();

    logger.LogInformation(
      "Job {JobName} closed {MatchCount} matches starting before cutoff {Cutoff}",
      nameof(CloseStartingSoonMatches),
      matchesToClose.Count,
      cutoff);
  }





  [AutomaticRetry(Attempts = 1)]
  public async Task GetBetclicGames()
  {
    logger.LogInformation(
      "Starting job {JobName} to process upcoming Betclic games",
      nameof(GetBetclicGames));

    var upcommingGames = await mediator.Send(new UpdateMatchesCommand());
    logger.LogInformation(
      "Job {JobName} received {GameCount} upcoming Betclic games",
      nameof(GetBetclicGames),
      upcommingGames.Count);
  }

  public async Task ScheduleBettingOddsJob()
  {
    logger.LogInformation(
      "Starting job {JobName} to schedule betting odds for upcoming matches",
      nameof(ScheduleBettingOddsJob));

    var upcommingGames = await db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming && m.BetclicUrl != null)
      .Select(m => new { m.BetclicUrl })
      .ToListAsync();

    logger.LogInformation(
      "Job {JobName} found {MatchCount} upcoming matches with Betclic URL to schedule",
      nameof(ScheduleBettingOddsJob),
      upcommingGames.Count);

    foreach (var match in upcommingGames)
    {
      var delay = TimeSpan.FromSeconds(Random.Shared.Next(0, 300));
      BackgroundJob.Schedule<JobService>(
          js => js.GetBettingOdds(match.BetclicUrl!),
          delay);
    }

    logger.LogInformation(
      "Job {JobName} scheduled {JobCount} betting odds jobs",
      nameof(ScheduleBettingOddsJob),
      upcommingGames.Count);
  }

  [AutomaticRetry(Attempts = 3)]
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

        var eventJson = JsonSerializer.Serialize(ev, JsonOptions);

        snapshot.Rows.Add(new BettingOddsSnapshotRow
        {
          EventJson = eventJson,
          EventType = eventType.Value
        });
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



public static class Head2HeadQueryableExtensions
{
  public static IQueryable<Head2Head> ForClubs(this IQueryable<Head2Head> query, int club1Id, int club2Id)
  {
    var (team1Id, team2Id) = Head2Head.NormalizeClubIds(club1Id, club2Id);
    return query.Where(h => h.Team1Id == team1Id && h.Team2Id == team2Id);
  }
}

public static class MatchQueryableExtensions
{
  /// <summary>
  /// Filters Match to rows where the two clubs are home and away (order-independent).
  /// </summary>
  public static IQueryable<Match> ForClubs(this IQueryable<Match> query, int club1Id, int club2Id) =>
    query.Where(m =>
      (m.HomeClubId == club1Id && m.AwayClubId == club2Id) ||
      (m.HomeClubId == club2Id && m.AwayClubId == club1Id));
}