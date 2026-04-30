using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Betting.SettlePendingBetSelections;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class BetslipSettlementJobService(
  IMediator mediator,
  ILogger<BetslipSettlementJobService> logger)
{
  [AutomaticRetry(Attempts = 3)]
  public async Task ResolveBetslipStatuses()
  {
    logger.LogInformation(
      "Starting job {JobName} to resolve pending betslip statuses",
      nameof(ResolveBetslipStatuses));

    await mediator.Send(new SettlePendingBetSelectionsCommand(), CancellationToken.None);

    logger.LogInformation(
      "Job {JobName} completed resolving pending betslip statuses",
      nameof(ResolveBetslipStatuses));
  }
}
