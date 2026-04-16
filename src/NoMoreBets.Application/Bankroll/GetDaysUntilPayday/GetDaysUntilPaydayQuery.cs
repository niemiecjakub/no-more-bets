using MediatR;
using Microsoft.Extensions.Logging;

namespace NoMoreBets.Application.Bankroll.GetDaysUntilPayday;

public record GetDaysUntilPaydayQuery : IRequest<int>;

/// <summary>
/// Paydays are the last calendar day of each month (UTC). Returns 0 on that day; otherwise whole days until then.
/// </summary>
public sealed class GetDaysUntilPaydayHandler(ILogger<GetDaysUntilPaydayHandler>? logger = null) : IRequestHandler<GetDaysUntilPaydayQuery, int>
{
  public Task<int> Handle(GetDaysUntilPaydayQuery request, CancellationToken cancellationToken)
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var lastThisMonth = LastDayOfMonth(today.Year, today.Month);

    if (today == lastThisMonth)
    {
      return Task.FromResult(0);
    }

    var days = lastThisMonth.DayNumber - today.DayNumber;
    return Task.FromResult(days);
  }

  private static DateOnly LastDayOfMonth(int year, int month) =>
    new(year, month, DateTime.DaysInMonth(year, month));
}
