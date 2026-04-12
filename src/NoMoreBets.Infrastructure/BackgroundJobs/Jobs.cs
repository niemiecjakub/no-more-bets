using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Bankroll.ApplyPayday;
using NoMoreBets.Application.Bankroll.GetDaysUntilPayday;
using NoMoreBets.Application.Betting.GetBetEvents;
using NoMoreBets.Application.Betting.SettlePendingBetSelections;
using NoMoreBets.Application.Betting.UpdateMatches;
using NoMoreBets.Application.Clubs.UpdateDailySummary;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues.UpdateTable;
using NoMoreBets.Application.Matches.UpdateLineup;
using NoMoreBets.Application.Matches.UpdateHeadToHead;
using NoMoreBets.Application.Matches.UpdateMatchPreview;
using NoMoreBets.Application.Matches.UpdateUpcomming;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Persistence;
using NoMoreBets.Infrastructure.Scraping.External.Fotmob;
using NoMoreBets.Application.Clubs.GetOverview;
using NoMoreBets.Application.Common.Dto.Leagues;
using NoMoreBets.Application.Matches.UpdateMatchDetails;
using NoMoreBets.Application.Matches.GetMatchPrediction;
using NoMoreBets.Infrastructure.Scraping.External.SoccerData;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public class JobService(
  IMediator mediator,
  AppDbContext db,
  IFotmobConstants fotmobConstants,
  SoccerDataClient soccerDataClient,
  ILogger<JobService> logger)
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

  [AutomaticRetry(Attempts = 3)]
  public async Task ApplyPaydayIfDue()
  {
    var daysUntilPayday = await mediator.Send(new GetDaysUntilPaydayQuery());
    if (daysUntilPayday != 0)
    {
      logger.LogInformation(
        "Job {JobName} skipped: {DaysUntilPayday} day(s) until payday",
        nameof(ApplyPaydayIfDue),
        daysUntilPayday);
      return;
    }

    logger.LogInformation("Job {JobName}: payday is today; applying salary", nameof(ApplyPaydayIfDue));
    await mediator.Send(new ApplyPaydayCommand());
  }

  /// <summary>
  /// Daily job: for all upcoming matches with SoccerdataId, enqueue RefreshHead2HeadStatistics;
  /// for those without a MatchPreview record, enqueue GetUpcommingSoccerdataMatchePreview.
  /// </summary>
  [AutomaticRetry(Attempts = 3)]
  public async Task RefreshUpcomingMatchPreviewsAndHead2Head()
  {
    logger.LogInformation(
      "Starting job {JobName} to schedule preview and head-to-head jobs for upcoming matches",
      nameof(RefreshUpcomingMatchPreviewsAndHead2Head));

    var upcomingWithSoccerdataId = await db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming && m.SoccerdataId != null)
      .Select(m => new { m.Id, m.HomeClubId, m.AwayClubId, m.SoccerdataId })
      .ToListAsync();

    var upcomingMatchIds = upcomingWithSoccerdataId.Select(m => m.Id).ToList();
    var matchIdsWithPreview = (await db.MatchPreview
      .Where(mp => upcomingMatchIds.Contains(mp.MatchId))
      .Select(mp => mp.MatchId)
      .ToListAsync()).ToHashSet();

    var enqueuedH2H = 0;
    var enqueuedPreview = 0;

    foreach (var match in upcomingWithSoccerdataId)
    {
      BackgroundJob.Enqueue(() => RefreshHead2HeadStatistics(match.HomeClubId, match.AwayClubId));
      enqueuedH2H++;

      if (!matchIdsWithPreview.Contains(match.Id))
      {
        var delay = TimeSpan.FromSeconds(Random.Shared.Next(0, 1500));
        BackgroundJob.Schedule<JobService>(js => js.GetUpcommingSoccerdataMatchePreview(match.SoccerdataId!.Value), delay);
        enqueuedPreview++;
      }
    }

    logger.LogInformation(
      "Job {JobName} enqueued {H2HCount} head-to-head jobs and {PreviewCount} preview jobs for {MatchCount} upcoming matches",
      nameof(RefreshUpcomingMatchPreviewsAndHead2Head),
      enqueuedH2H,
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
          .Where(m => m.MatchStatus == Domain.Enums.MatchStatus.Finished)
          .OrderByDescending(m => m.MatchDate)
          .Select(m => (DateTime?)m.MatchDate)
          .FirstOrDefaultAsync();

      shouldUpdate = lastFinishedGameDate > h2h.UpdatedAt;
    }

    if (shouldUpdate)
    {
      var delay = TimeSpan.FromSeconds(Random.Shared.Next(0, 1500));
      BackgroundJob.Schedule<JobService>(js => js.RefreshHead2HeadData(homeClubSoccerdataId, awayClubSoccerdataId), delay);
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

  /// <summary>
  /// Enqueues UpdateDailySummary for every club in the database. Run daily (e.g. at 14:00).
  /// Uses staggered delays so club summary and match-detail jobs do not collide.
  /// </summary>
  [AutomaticRetry(Attempts = 1)]
  public async Task UpdateClubOverview()
  {
    logger.LogInformation(
      "Starting job {JobName} to enqueue daily summary updates for all clubs",
      nameof(UpdateClubOverview));

    var clubs = await db.Club.Select(c => new { c.Id, c.Name }).ToListAsync();

    logger.LogInformation(
      "Job {JobName} found {ClubCount} clubs to update",
      nameof(UpdateClubOverview),
      clubs.Count);

    var fotmobMatchUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var club in clubs)
    {
      var fotmobTeam = fotmobConstants.GetTeamByName(club.Name);
      if (fotmobTeam == null)
      {
        logger.LogDebug(
          "Job {JobName} skipping club {ClubId} ({ClubName}): no Fotmob team mapping",
          nameof(UpdateClubOverview),
          club.Id,
          club.Name);
        continue;
      }

      ClubOverview clubOverview;
      try
      {
        clubOverview = await mediator.Send(new GetClubOverviewQuery(fotmobTeam.Id)).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        logger.LogWarning(
          ex,
          "Job {JobName} failed to get overview for club {ClubId} ({ClubName}), FotmobTeamId={FotmobTeamId}",
          nameof(UpdateClubOverview),
          club.Id,
          club.Name,
          fotmobTeam.Id);
        continue;
      }

      if (!string.IsNullOrWhiteSpace(clubOverview.DailySummary))
      {
        var delay = TimeSpan.FromSeconds(Random.Shared.Next(0, 300));
        BackgroundJob.Schedule<JobService>(js => js.UpdateDailySummaryForClub(club.Id, clubOverview.DailySummary), delay);
      }

      foreach (var recentGame in clubOverview.RecentGames)
      {
        fotmobMatchUrls.Add(recentGame.GameUrl);
      }
    }

    foreach (var url in fotmobMatchUrls)
    {
      var delay = TimeSpan.FromSeconds(Random.Shared.Next(500, 2000));
      BackgroundJob.Schedule<JobService>(js => js.UpdateMatchDetails(url), delay);
    }
  }

  public async Task UpdateDailySummaryForClub(int clubId, string summary)
  {
    await mediator.Send(new UpdateDailySummaryCommand(clubId, summary));
  }

  public async Task UpdateMatchDetails(string fotmobMatchUrl)
  {
    await mediator.Send(new UpdateMatchDetailsCommand(fotmobMatchUrl));
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

  [AutomaticRetry(Attempts = 3)]
  public async Task FillMissingFinishedMatchScoresFromSoccerData()
  {
    logger.LogInformation(
      "Starting job {JobName} to fill missing scores for finished matches",
      nameof(FillMissingFinishedMatchScoresFromSoccerData));

    var dbMatches = await db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Finished)
      .Where(m => m.HomeGoals == null || m.AwayGoals == null)
      .Where(m => m.SoccerdataId.HasValue)
      .ToListAsync();

    if (dbMatches.Count == 0)
    {
      logger.LogInformation(
        "Job {JobName} found no finished matches with missing scores",
        nameof(FillMissingFinishedMatchScoresFromSoccerData));
      return;
    }

    var premierLeagueMatches = await soccerDataClient.GetMatchesAsync(
      leagueId: SoccerDataConstants.PremierLeagueId);

    var soccerDataMatchesById = premierLeagueMatches
      .SelectMany(league => league.Stage)
      .SelectMany(stage => stage.Matches)
      .GroupBy(match => match.Id)
      .ToDictionary(g => g.Key, g => g.First());

    var updatedCount = 0;
    foreach (var dbMatch in dbMatches)
    {
      var soccerdataId = dbMatch.SoccerdataId!.Value;
      if (!soccerDataMatchesById.TryGetValue(soccerdataId, out var soccerDataMatch))
        continue;

      dbMatch.HomeGoals = soccerDataMatch.Goals.HomeFtGoals;
      dbMatch.AwayGoals = soccerDataMatch.Goals.AwayFtGoals;
      updatedCount++;
    }

    if (updatedCount > 0)
    {
      await db.SaveChangesAsync();
      await mediator.Send(new SettlePendingBetSelectionsCommand(), CancellationToken.None);
      logger.LogInformation(
        "Job {JobName} ran pending bet settlement after score updates",
        nameof(FillMissingFinishedMatchScoresFromSoccerData));
    }

    logger.LogInformation(
      "Job {JobName} updated scores for {UpdatedCount} matches",
      nameof(FillMissingFinishedMatchScoresFromSoccerData),
      updatedCount);
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

  /// <summary>
  /// Finds all upcoming matches that are fully populated with preview, lineup,
  /// betting odds snapshot and head-to-head data, but do not yet have any
  /// MatchAnalysis records, and enqueues individual prediction jobs for them.
  /// </summary>
  public async Task GenerateMissingMatchPredictions()
  {
    logger.LogInformation(
      "Starting job {JobName} to enqueue match predictions for fully prepared upcoming matches without analysis",
      nameof(GenerateMissingMatchPredictions));

    var completeMatchIds = await db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming)
      .Where(m => db.MatchPreview.Any(mp => mp.MatchId == m.Id))
      .Where(m => db.Lineup.Any(l => l.MatchId == m.Id))
      .Where(m => db.BettingOddsSnapshot.Any(b => b.MatchId == m.Id))
      .Where(m => db.Head2Head.Any(h =>
        (h.Team1Id == m.HomeClubId && h.Team2Id == m.AwayClubId) ||
        (h.Team1Id == m.AwayClubId && h.Team2Id == m.HomeClubId)))
      .Select(m => m.Id)
      .ToListAsync();

    var completeSet = completeMatchIds.ToHashSet();

    var matchIdsWithAnalysis = await db.MatchAnalysis
      .Select(a => a.MatchId)
      .Distinct()
      .ToListAsync();
    var hasAnalysisSet = matchIdsWithAnalysis.ToHashSet();

    var matchIdsToAnalyse = completeSet
      .Where(id => !hasAnalysisSet.Contains(id))
      .ToList();

    logger.LogInformation(
      "Job {JobName} found {CompleteCount} fully prepared upcoming matches, {WithAnalysisCount} with existing analysis, {ToAnalyseCount} remaining to analyse",
      nameof(GenerateMissingMatchPredictions),
      completeSet.Count,
      hasAnalysisSet.Count,
      matchIdsToAnalyse.Count);

    if (matchIdsToAnalyse.Count == 0)
    {
      logger.LogInformation(
        "Job {JobName} found no matches requiring new predictions. Exiting.",
        nameof(GenerateMissingMatchPredictions));
      return;
    }

    foreach (var matchId in matchIdsToAnalyse)
    {
      var delay = TimeSpan.FromSeconds(Random.Shared.Next(0, 1000));
      BackgroundJob.Schedule<JobService>(js => js.RunMatchPrediction(matchId), delay);
    }

    logger.LogInformation(
      "Job {JobName} scheduled prediction jobs with random delays for {JobCount} matches",
      nameof(GenerateMissingMatchPredictions),
      matchIdsToAnalyse.Count);
  }

  /// <summary>
  /// Executes match prediction generation for a single match via MediatR.
  /// </summary>
  [AutomaticRetry(Attempts = 0)]
  public async Task RunMatchPrediction(int matchId)
  {
    logger.LogInformation(
      "Starting job {JobName} for MatchId {MatchId}",
      nameof(RunMatchPrediction),
      matchId);

    await mediator.Send(new GetMatchPredictionCommand(matchId));

    logger.LogInformation(
      "Completed job {JobName} for MatchId {MatchId}",
      nameof(RunMatchPrediction),
      matchId);
  }
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