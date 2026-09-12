using Hangfire;
using Microsoft.Extensions.Logging;
using NoMoreBets.Infrastructure.AI.Phases.MemoryCleanup;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class MemoryCleanupCronService(
  MemoryCleanupPhaseRunner memoryCleanupPhaseRunner,
  ILogger<MemoryCleanupCronService> logger)
{
  [AutomaticRetry(Attempts = 1)]
  public async Task RunAsync()
  {
    logger.LogInformation("Starting scheduled memory cleanup agent phase");
    await memoryCleanupPhaseRunner.RunAsync(CancellationToken.None).ConfigureAwait(false);
    logger.LogInformation("Finished scheduled memory cleanup agent phase");
  }
}
