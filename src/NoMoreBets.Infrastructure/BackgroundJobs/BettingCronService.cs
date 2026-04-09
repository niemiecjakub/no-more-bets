using Hangfire;
using Microsoft.Extensions.Logging;
using NoMoreBets.Infrastructure.AI.Provider;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class BettingCronService(Runner runner, ILogger<BettingCronService> logger)
{
  [AutomaticRetry(Attempts = 3)]
  public async Task RunAsync()
  {
    logger.LogInformation("Starting scheduled betting execution agent phase");
    await runner.RunBettingExecutionPhaseAsync(CancellationToken.None).ConfigureAwait(false);
    logger.LogInformation("Finished scheduled betting execution agent phase");
  }
}
