using MediatR;

namespace NoMoreBets.Application.Bankroll.GetDaysUntilPayday;

public record GetDaysUntilPaydayQuery : IRequest<int>;

/// <summary>
/// Paydays are the last calendar day of each month (UTC). On month-end, the next payday is the last day of the following month.
/// </summary>
public sealed class GetDaysUntilPaydayHandler : IRequestHandler<GetDaysUntilPaydayQuery, int>
{
  public Task<int> Handle(GetDaysUntilPaydayQuery request, CancellationToken cancellationToken)
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var lastThisMonth = LastDayOfMonth(today.Year, today.Month);

    DateOnly nextPayday;
    if (today < lastThisMonth)
    {
      nextPayday = lastThisMonth;
    }
    else
    {
      var nextMonth = today.AddMonths(1);
      nextPayday = LastDayOfMonth(nextMonth.Year, nextMonth.Month);
    }

    var days = nextPayday.DayNumber - today.DayNumber;
    return Task.FromResult(days);
  }

  private static DateOnly LastDayOfMonth(int year, int month) =>
    new(year, month, DateTime.DaysInMonth(year, month));
}
