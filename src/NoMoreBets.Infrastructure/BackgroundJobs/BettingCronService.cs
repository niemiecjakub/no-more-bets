using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Betting.GetMatchesAvailableForBetting;
using NoMoreBets.Infrastructure.AI.Provider;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class BettingCronService(Runner runner, IMediator mediator, ILogger<BettingCronService> logger)
{
  [AutomaticRetry(Attempts = 1)]
  public async Task RunAsync()
  {
    var matches = await mediator
      .Send(new GetMatchesAvailableForBettingQuery(), CancellationToken.None)
      .ConfigureAwait(false);
    if (matches.Count == 0)
    {
      logger.LogInformation("Skipping scheduled betting execution agent phase: no matches available for betting");
      return;
    }

    logger.LogInformation("Starting scheduled betting execution agent phase");
    await runner.RunBettingExecutionPhaseAsync(CancellationToken.None).ConfigureAwait(false);
    logger.LogInformation("Finished scheduled betting execution agent phase");
  }
}
