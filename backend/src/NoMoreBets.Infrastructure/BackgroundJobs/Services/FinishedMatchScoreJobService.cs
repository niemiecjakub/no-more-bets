using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Betting.SettlePendingBetSelections;
using NoMoreBets.Application.Common.Dto.Matches;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Application.Common.SoccerData;
using NoMoreBets.Application.Matches;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Infrastructure.Persistence;
using NoMoreBets.Infrastructure.Scraping.External.SoccerData;
using DomainMatch = NoMoreBets.Domain.Matches.Match;
using SoccerDataMatch = NoMoreBets.Application.Common.Dto.Matches.Match;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class FinishedMatchScoreJobService(
  AppDbContext db,
  SoccerDataClient soccerDataClient,
  IMatchMatcher matchMatcher,
  IMatchResultsProvider matchResultsProvider,
  IMatchEventsProvider matchEventsProvider,
  IMediator mediator,
  ILogger<FinishedMatchScoreJobService> logger)
{
  [AutomaticRetry(Attempts = 3)]
  public async Task FillMissingFinishedMatchScoresFromSoccerData()
  {
    logger.LogInformation(
      "Starting job {JobName} to fill missing scores and events for finished matches",
      nameof(FillMissingFinishedMatchScoresFromSoccerData));

    var dbMatchesWithLeague = await db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Finished)
      .Where(m => m.HomeGoals == null || m.AwayGoals == null || !m.MatchEvents.Any())
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .Select(m => new
      {
        Match = m,
        LeagueSoccerdataId = m.Stage != null ? (int?)m.Stage.Season.League.SoccerdataId : null,
        LeagueSlug = m.Stage != null ? m.Stage.Season.League.Slug : null,
        HasEvents = m.MatchEvents.Any()
      })
      .ToListAsync();

    if (dbMatchesWithLeague.Count == 0)
    {
      logger.LogInformation(
        "Job {JobName} found no finished matches with missing scores or events",
        nameof(FillMissingFinishedMatchScoresFromSoccerData));
      return;
    }

    var missingLeagueContextCount = dbMatchesWithLeague.Count(x => !x.LeagueSoccerdataId.HasValue);
    if (missingLeagueContextCount > 0)
    {
      logger.LogWarning(
        "Job {JobName} skipped {SkippedCount} matches because Stage/League context is missing",
        nameof(FillMissingFinishedMatchScoresFromSoccerData),
        missingLeagueContextCount);
    }

    var updatedScoreCount = 0;
    var flashscoreScoreCount = 0;
    var flashscoreEventCount = 0;
    var addedEventCount = 0;
    var resolvedSoccerdataIdCount = 0;
    var matchIdsWithEventsThisRun = new HashSet<int>();
    var matchesByLeague = dbMatchesWithLeague
      .Where(x => x.LeagueSoccerdataId.HasValue)
      .GroupBy(x => x.LeagueSoccerdataId!.Value);

    foreach (var leagueGroup in matchesByLeague)
    {
      var leagueMatches = await soccerDataClient.GetMatchesAsync(leagueId: leagueGroup.Key);
      var allSoccerDataMatches = leagueMatches
        .SelectMany(league => league.Stage)
        .SelectMany(stage => stage.Matches)
        .ToList();
      var soccerDataMatchesById = allSoccerDataMatches
        .GroupBy(match => match.Id)
        .ToDictionary(g => g.Key, g => g.First());

      foreach (var item in leagueGroup)
      {
        var soccerDataMatch = ResolveSoccerDataMatch(
          item.Match,
          soccerDataMatchesById,
          allSoccerDataMatches,
          matchMatcher);

        if (soccerDataMatch is null)
          continue;

        if (!item.Match.SoccerdataId.HasValue)
        {
          var conflict = await db.Match.AnyAsync(
            m => m.SoccerdataId == soccerDataMatch.Id && m.Id != item.Match.Id);
          if (conflict)
          {
            logger.LogWarning(
              "Job {JobName} skipped assigning SoccerdataId {SoccerdataId} to MatchId={MatchId}: another match already has this ID",
              nameof(FillMissingFinishedMatchScoresFromSoccerData),
              soccerDataMatch.Id,
              item.Match.Id);
            continue;
          }

          item.Match.SoccerdataId = soccerDataMatch.Id;
          resolvedSoccerdataIdCount++;
        }

        if (!soccerDataMatch.IsFinished)
          continue;

        if (item.Match.HomeGoals is null || item.Match.AwayGoals is null)
        {
          item.Match.HomeGoals = soccerDataMatch.Goals.HomeFtGoals;
          item.Match.AwayGoals = soccerDataMatch.Goals.AwayFtGoals;
          updatedScoreCount++;
        }

        if (!item.HasEvents)
        {
          var added = await SoccerDataMatchEventSync.AddMissingEventsAsync(
            db,
            item.Match,
            soccerDataMatch.Events,
            logger);
          addedEventCount += added;
          if (added > 0)
            matchIdsWithEventsThisRun.Add(item.Match.Id);
        }
      }
    }

    var stillNeedingFlashscore = dbMatchesWithLeague
      .Where(x => x.LeagueSlug is not null)
      .Where(x =>
        x.Match.HomeGoals is null
        || x.Match.AwayGoals is null
        || (!x.HasEvents && !matchIdsWithEventsThisRun.Contains(x.Match.Id)))
      .GroupBy(x => x.LeagueSlug!);

    foreach (var slugGroup in stillNeedingFlashscore)
    {
      var candidates = slugGroup
        .Select(x => (x.Match, NeedsEvents: !x.HasEvents && !matchIdsWithEventsThisRun.Contains(x.Match.Id)))
        .ToList();

      var (scoresFilled, eventsAdded) = await FillFromFlashscoreAsync(slugGroup.Key, candidates);
      flashscoreScoreCount += scoresFilled;
      flashscoreEventCount += eventsAdded;
      updatedScoreCount += scoresFilled;
      addedEventCount += eventsAdded;
    }

    if (updatedScoreCount > 0 || addedEventCount > 0 || resolvedSoccerdataIdCount > 0)
    {
      await db.SaveChangesAsync();

      if (updatedScoreCount > 0)
      {
        await mediator.Send(new SettlePendingBetSelectionsCommand(), CancellationToken.None);
        logger.LogInformation(
          "Job {JobName} ran pending bet settlement after score updates",
          nameof(FillMissingFinishedMatchScoresFromSoccerData));
      }
    }

    logger.LogInformation(
      "Job {JobName} resolved SoccerdataId for {ResolvedSoccerdataIdCount} matches, updated scores for {UpdatedScoreCount} matches ({FlashscoreScoreCount} via Flashscore fallback) and added {AddedEventCount} match events ({FlashscoreEventCount} via Flashscore fallback)",
      nameof(FillMissingFinishedMatchScoresFromSoccerData),
      resolvedSoccerdataIdCount,
      updatedScoreCount,
      flashscoreScoreCount,
      addedEventCount,
      flashscoreEventCount);
  }

  private async Task<(int ScoresFilled, int EventsAdded)> FillFromFlashscoreAsync(
    string leagueSlug,
    IReadOnlyList<(DomainMatch Match, bool NeedsEvents)> candidates)
  {
    if (candidates.Count == 0)
      return (0, 0);

    var results = await matchResultsProvider.GetFinishedResultsAsync(leagueSlug);
    if (results.Count == 0)
      return (0, 0);

    var scoresFilled = 0;
    var eventsAdded = 0;
    foreach (var (match, needsEvents) in candidates)
    {
      var needsScores = match.HomeGoals is null || match.AwayGoals is null;
      if (!needsScores && !needsEvents)
        continue;

      var matchDate = DateOnly.FromDateTime(DateTime.SpecifyKind(match.MatchDate, DateTimeKind.Utc));
      var dayCandidates = new List<(string HomeName, string AwayName, FinishedMatchResult Value)>();
      foreach (var result in results)
      {
        if (result.MatchDate != matchDate)
          continue;

        dayCandidates.Add((result.HomeTeam, result.AwayTeam, result));
      }

      var best = matchMatcher.FindBestMatch(
        match.HomeClub.Name,
        match.AwayClub.Name,
        dayCandidates);

      if (best is null)
        continue;

      if (needsScores)
      {
        match.HomeGoals = best.HomeGoals;
        match.AwayGoals = best.AwayGoals;
        scoresFilled++;
      }

      if (needsEvents && !string.IsNullOrWhiteSpace(best.DetailUrl))
      {
        var flashscoreEvents = await matchEventsProvider.GetMatchEventsAsync(best.DetailUrl);
        var added = await SoccerDataMatchEventSync.AddMissingEventsAsync(
          db,
          match,
          flashscoreEvents,
          logger);
        eventsAdded += added;
      }
    }

    return (scoresFilled, eventsAdded);
  }

  private static SoccerDataMatch? ResolveSoccerDataMatch(
    DomainMatch dbMatch,
    IReadOnlyDictionary<int, SoccerDataMatch> soccerDataMatchesById,
    IReadOnlyList<SoccerDataMatch> allSoccerDataMatches,
    IMatchMatcher matchMatcher)
  {
    if (dbMatch.SoccerdataId is { } soccerdataId &&
        soccerDataMatchesById.TryGetValue(soccerdataId, out var byId))
    {
      return byId;
    }

    var matchDateUtc = DateTime.SpecifyKind(dbMatch.MatchDate, DateTimeKind.Utc).Date;
    var candidates = new List<(string HomeName, string AwayName, SoccerDataMatch Value)>();
    foreach (var soccerDataMatch in allSoccerDataMatches)
    {
      if (!SoccerDataKickoffDateParser.TryParse(soccerDataMatch.Date, soccerDataMatch.Time, out var kickoffUtc))
        continue;

      if (kickoffUtc.Date != matchDateUtc)
        continue;

      candidates.Add((soccerDataMatch.Teams.Home.Name, soccerDataMatch.Teams.Away.Name, soccerDataMatch));
    }

    return matchMatcher.FindBestMatch(dbMatch.HomeClub.Name, dbMatch.AwayClub.Name, candidates);
  }
}
