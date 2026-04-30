using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Matches.GetMatchPrediction;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class MatchPredictionJobService(
  IMediator mediator,
  AppDbContext db,
  ILogger<MatchPredictionJobService> logger)
{
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
      BackgroundJob.Schedule<MatchPredictionJobService>(js => js.RunMatchPrediction(matchId), delay);
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
