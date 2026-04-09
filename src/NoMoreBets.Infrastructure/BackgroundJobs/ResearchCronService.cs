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
  [AutomaticRetry(Attempts = 3)]
  public async Task RunAsync()
  {
    logger.LogInformation("Starting scheduled research agent phase");
    var matches = await unitOfWork.Matches
      .GetUpcomingReadyForPredictionWithoutResearchAnalysisAsync(CancellationToken.None)
      .ConfigureAwait(false);

    for (var i = 0; i < matches.Count; i++)
    {
      await runner.RunResearchPhaseAsync(matches[i], CancellationToken.None).ConfigureAwait(false);

      if (i < matches.Count - 1)
      {
        await Task.Delay(TimeSpan.FromMinutes(1), CancellationToken.None).ConfigureAwait(false);
      }
    }

    logger.LogInformation("Finished scheduled research agent phase");
  }
}
