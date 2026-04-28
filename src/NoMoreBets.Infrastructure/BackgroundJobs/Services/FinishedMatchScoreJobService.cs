using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Betting.SettlePendingBetSelections;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Infrastructure.Persistence;
using NoMoreBets.Infrastructure.Scraping.External.SoccerData;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class FinishedMatchScoreJobService(
  AppDbContext db,
  SoccerDataClient soccerDataClient,
  IMediator mediator,
  ILogger<FinishedMatchScoreJobService> logger)
{
  [AutomaticRetry(Attempts = 3)]
  public async Task FillMissingFinishedMatchScoresFromSoccerData()
  {
    logger.LogInformation(
      "Starting job {JobName} to fill missing scores for finished matches",
      nameof(FillMissingFinishedMatchScoresFromSoccerData));

    var dbMatchesWithLeague = await db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Finished)
      .Where(m => m.HomeGoals == null || m.AwayGoals == null)
      .Where(m => m.SoccerdataId.HasValue)
      .Select(m => new
      {
        Match = m,
        LeagueSoccerdataId = m.Stage != null ? (int?)m.Stage.Season.League.SoccerdataId : null
      })
      .ToListAsync();

    if (dbMatchesWithLeague.Count == 0)
    {
      logger.LogInformation(
        "Job {JobName} found no finished matches with missing scores",
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

    var updatedCount = 0;
    var matchesByLeague = dbMatchesWithLeague
      .Where(x => x.LeagueSoccerdataId.HasValue)
      .GroupBy(x => x.LeagueSoccerdataId!.Value);

    foreach (var leagueGroup in matchesByLeague)
    {
      var leagueMatches = await soccerDataClient.GetMatchesAsync(leagueId: leagueGroup.Key);
      var soccerDataMatchesById = leagueMatches
        .SelectMany(league => league.Stage)
        .SelectMany(stage => stage.Matches)
        .GroupBy(match => match.Id)
        .ToDictionary(g => g.Key, g => g.First());

      foreach (var item in leagueGroup)
      {
        var soccerdataId = item.Match.SoccerdataId!.Value;
        if (!soccerDataMatchesById.TryGetValue(soccerdataId, out var soccerDataMatch))
          continue;

        item.Match.HomeGoals = soccerDataMatch.Goals.HomeFtGoals;
        item.Match.AwayGoals = soccerDataMatch.Goals.AwayFtGoals;
        updatedCount++;
      }
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
}
