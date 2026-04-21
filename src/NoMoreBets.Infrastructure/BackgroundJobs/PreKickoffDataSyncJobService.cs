using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Matches.UpdateHeadToHead;
using NoMoreBets.Application.Matches.UpdateMatchPreview;
using NoMoreBets.Application.Matches.UpdateUpcomming;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

/// <summary>
/// Syncs league-driven upcoming fixtures, head-to-head history, and match previews ahead of kickoff.
/// </summary>
public sealed class PreKickoffDataSyncJobService(
  IMediator mediator,
  AppDbContext db,
  ILogger<PreKickoffDataSyncJobService> logger)
{
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
  }

  [AutomaticRetry(Attempts = 1)]
  public async Task GetUpcommingSoccerdataMatchesForAllLeagues()
  {
    var leagues = await db.League
      .Where(l => l.SoccerdataId > 0)
      .Select(l => new { l.Id, l.Name, l.SoccerdataId })
      .ToListAsync();

    if (leagues.Count == 0)
    {
      logger.LogWarning(
        "Job {JobName} found no leagues with SoccerdataId configured",
        nameof(GetUpcommingSoccerdataMatchesForAllLeagues));
      return;
    }

    logger.LogInformation(
      "Job {JobName} will sync upcoming matches for {LeagueCount} leagues",
      nameof(GetUpcommingSoccerdataMatchesForAllLeagues),
      leagues.Count);

    foreach (var league in leagues)
    {
      logger.LogInformation(
        "Job {JobName} syncing league {LeagueId} ({LeagueName}) with SoccerdataId={SoccerdataLeagueId}",
        nameof(GetUpcommingSoccerdataMatchesForAllLeagues),
        league.Id,
        league.Name,
        league.SoccerdataId);
      await GetUpcommingSoccerdataMatches(league.SoccerdataId);
    }
  }

  /// <summary>
  /// Daily job: for upcoming matches with a match SoccerdataId, load home/away club Soccerdata ids
  /// and schedule <see cref="RefreshHead2HeadStatistics"/> per match with a random delay (0–1500s).
  /// </summary>
  [AutomaticRetry(Attempts = 1)]
  public async Task ScheduleRefreshHead2HeadForUpcomingMatches()
  {
    logger.LogInformation(
      "Starting job {JobName} to schedule head-to-head jobs for upcoming matches",
      nameof(ScheduleRefreshHead2HeadForUpcomingMatches));

    var upcoming = await db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming)
      .Select(m => new
      {
        m.HomeClubId,
        m.AwayClubId,
        HomeClubSoccerdataId = m.HomeClub.SoccerdataId,
        AwayClubSoccerdataId = m.AwayClub.SoccerdataId
      })
      .ToListAsync();

    var scheduled = 0;
    foreach (var match in upcoming)
    {
      var delay = TimeSpan.FromSeconds(Random.Shared.Next(0, 1500));
      BackgroundJob.Schedule<PreKickoffDataSyncJobService>(js =>
        js.RefreshHead2HeadStatistics(match.HomeClubId, match.AwayClubId), delay);
      scheduled++;
    }

    logger.LogInformation(
      "Job {JobName} scheduled {ScheduledCount} head-to-head jobs for {MatchCount} upcoming matches (each row includes home/away club Soccerdata ids from DB)",
      nameof(ScheduleRefreshHead2HeadForUpcomingMatches),
      scheduled,
      upcoming.Count);
  }

  /// <summary>
  /// Daily job: for upcoming matches with SoccerdataId and no <see cref="MatchPreview"/> row,
  /// enqueue <see cref="GetUpcommingSoccerdataMatchePreview"/> with a random delay.
  /// </summary>
  [AutomaticRetry(Attempts = 1)]
  public async Task ScheduleMissingPreviewJobsForUpcomingMatches()
  {
    logger.LogInformation(
      "Starting job {JobName} to schedule preview jobs for upcoming matches without a preview",
      nameof(ScheduleMissingPreviewJobsForUpcomingMatches));

    var upcomingWithSoccerdataId = await db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming && m.SoccerdataId != null)
      .Select(m => new { m.Id, m.SoccerdataId })
      .ToListAsync();

    var upcomingMatchIds = upcomingWithSoccerdataId.Select(m => m.Id).ToList();
    var matchIdsWithPreview = (await db.MatchPreview
      .Where(mp => upcomingMatchIds.Contains(mp.MatchId))
      .Select(mp => mp.MatchId)
      .ToListAsync()).ToHashSet();

    var enqueuedPreview = 0;
    foreach (var match in upcomingWithSoccerdataId)
    {
      if (!matchIdsWithPreview.Contains(match.Id))
      {
        var delay = TimeSpan.FromSeconds(Random.Shared.Next(0, 1500));
        BackgroundJob.Schedule<PreKickoffDataSyncJobService>(js => js.GetUpcommingSoccerdataMatchePreview(match.SoccerdataId!.Value), delay);
        enqueuedPreview++;
      }
    }

    logger.LogInformation(
      "Job {JobName} enqueued {PreviewCount} preview jobs for {MatchCount} upcoming matches",
      nameof(ScheduleMissingPreviewJobsForUpcomingMatches),
      enqueuedPreview,
      upcomingWithSoccerdataId.Count);
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
          .Where(m => m.MatchStatus == MatchStatus.Finished)
          .OrderByDescending(m => m.MatchDate)
          .Select(m => (DateTime?)m.MatchDate)
          .FirstOrDefaultAsync();

      shouldUpdate = lastFinishedGameDate > h2h.UpdatedAt;
    }

    if (shouldUpdate)
    {
      await mediator.Send(new UpdateHeadToHeadCommand(homeClubSoccerdataId, awayClubSoccerdataId));
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
}
