using Hangfire;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class MemoryCleanupCronService(
  IAgentPhaseRunner agentPhaseRunner,
  ILogger<MemoryCleanupCronService> logger)
{
  [AutomaticRetry(Attempts = 1)]
  public async Task RunAsync()
  {
    logger.LogInformation("Starting scheduled memory cleanup agent phase");
    await agentPhaseRunner.RunMemoryCleanupPhaseAsync(CancellationToken.None).ConfigureAwait(false);
    logger.LogInformation("Finished scheduled memory cleanup agent phase");
  }
}
