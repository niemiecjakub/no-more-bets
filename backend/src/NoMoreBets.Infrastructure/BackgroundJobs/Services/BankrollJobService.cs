using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Bankroll.ApplyPayday;
using NoMoreBets.Application.Bankroll.GetDaysUntilPayday;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class BankrollJobService(IMediator mediator, ILogger<BankrollJobService> logger)
{
  [AutomaticRetry(Attempts = 3)]
  public async Task ApplyPaydayIfDue()
  {
    var daysUntilPayday = await mediator.Send(new GetDaysUntilPaydayQuery());
    if (daysUntilPayday != 0)
    {
      logger.LogInformation(
        "Job {JobName} skipped: {DaysUntilPayday} day(s) until payday",
        nameof(ApplyPaydayIfDue),
        daysUntilPayday);
      return;
    }

    logger.LogInformation("Job {JobName}: payday is today; applying salary", nameof(ApplyPaydayIfDue));
    await mediator.Send(new ApplyPaydayCommand());
  }
}
