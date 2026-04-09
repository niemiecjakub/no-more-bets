using Hangfire;
using Microsoft.Extensions.Logging;
using NoMoreBets.Infrastructure.AI.Provider;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class ResearchCronService(Runner runner, ILogger<ResearchCronService> logger)
{
  [AutomaticRetry(Attempts = 3)]
  public async Task RunAsync()
  {
    logger.LogInformation("Starting scheduled research agent phase");
    await runner.RunResearchPhaseAsync(CancellationToken.None).ConfigureAwait(false);
    logger.LogInformation("Finished scheduled research agent phase");
  }
}
