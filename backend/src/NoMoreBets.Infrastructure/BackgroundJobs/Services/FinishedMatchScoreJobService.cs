using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Betting.SettlePendingBetSelections;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Application.Common.SoccerData;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Persistence;
using NoMoreBets.Infrastructure.Scraping.External.SoccerData;
using SoccerDataMatch = NoMoreBets.Application.Common.Dto.Matches.Match;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class FinishedMatchScoreJobService(
  AppDbContext db,
  SoccerDataClient soccerDataClient,
  IMatchMatcher matchMatcher,
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
    var addedEventCount = 0;
    var resolvedSoccerdataIdCount = 0;
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

        if (soccerDataMatch.IsFinished)
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

        if (item.Match.HomeGoals is null || item.Match.AwayGoals is null)
        {
          item.Match.HomeGoals = soccerDataMatch.Goals.HomeFtGoals;
          item.Match.AwayGoals = soccerDataMatch.Goals.AwayFtGoals;
          updatedScoreCount++;
        }

        if (!item.HasEvents)
        {
          addedEventCount += await SoccerDataMatchEventSync.AddMissingEventsAsync(
            db,
            item.Match,
            soccerDataMatch.Events,
            logger);
        }
      }
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
      "Job {JobName} resolved SoccerdataId for {ResolvedSoccerdataIdCount} matches, updated scores for {UpdatedScoreCount} matches and added {AddedEventCount} match events",
      nameof(FillMissingFinishedMatchScoresFromSoccerData),
      resolvedSoccerdataIdCount,
      updatedScoreCount,
      addedEventCount);
  }

  private static SoccerDataMatch? ResolveSoccerDataMatch(
    Match dbMatch,
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
