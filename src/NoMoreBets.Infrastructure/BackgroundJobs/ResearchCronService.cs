using Hangfire;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Provider;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class ResearchCronService(
  Runner runner,
  IUnitOfWork unitOfWork,
  ILogger<ResearchCronService> logger)
{
  [AutomaticRetry(Attempts = 1)]
  public async Task RunAsync()
  {
    logger.LogInformation("Starting scheduled research agent phase");
    var matches = await unitOfWork.Matches
      .GetUpcomingReadyForPredictionWithoutResearchAnalysisAsync(CancellationToken.None)
      .ConfigureAwait(false);

    for (var i = 0; i < matches.Count; i++)
    {
      var matchId = matches[i].Id;
      BackgroundJob.Enqueue<ResearchCronService>(service => service.RunResearchPhaseForMatchAsync(matchId));

      if (i < matches.Count - 1)
      {
        await Task.Delay(TimeSpan.FromMinutes(4), CancellationToken.None).ConfigureAwait(false);
      }
    }

    logger.LogInformation("Finished scheduled research agent phase");
  }

  [AutomaticRetry(Attempts = 1)]
  public async Task RunResearchPhaseForMatchAsync(int matchId)
  {
    var match = await unitOfWork.Matches.GetMatchByIdAsync(matchId, CancellationToken.None).ConfigureAwait(false);

    if (match is null)
    {
      logger.LogWarning("Skipping research phase because match {MatchId} was not found", matchId);
      return;
    }

    await runner.RunResearchPhaseAsync(match, CancellationToken.None).ConfigureAwait(false);
  }
}
