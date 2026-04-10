using Hangfire;
using Microsoft.Extensions.Logging;
using NoMoreBets.Infrastructure.AI.Provider;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class ReflectionCronService(Runner runner, ILogger<ReflectionCronService> logger)
{
  [AutomaticRetry(Attempts = 3)]
  public async Task RunAsync()
  {
    logger.LogInformation("Starting scheduled reflection agent phase");
    //await runner.RunReflectionPhaseAsync(CancellationToken.None).ConfigureAwait(false);
    logger.LogInformation("Finished scheduled reflection agent phase");
  }
}
