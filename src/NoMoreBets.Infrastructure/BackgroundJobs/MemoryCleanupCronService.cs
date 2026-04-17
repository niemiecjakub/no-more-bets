using Hangfire;
using Microsoft.Extensions.Logging;
using NoMoreBets.Infrastructure.AI.Provider;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class MemoryCleanupCronService(Runner runner, ILogger<MemoryCleanupCronService> logger)
{
  [AutomaticRetry(Attempts = 1)]
  public async Task RunAsync()
  {
    logger.LogInformation("Starting scheduled memory cleanup agent phase");
    await runner.RunMemoryCleanupPhaseAsync(CancellationToken.None).ConfigureAwait(false);
    logger.LogInformation("Finished scheduled memory cleanup agent phase");
  }
}
