using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Application.Leagues;

/// <summary>
/// Season date window for scrape jobs. Null bounds are open-ended.
/// </summary>
public static class SeasonFetchWindow
{
  public const int Days = 7;

  /// <summary>Fetch when today is within [StartDate - <paramref name="daysBeforeStart"/>d, EndDate + <paramref name="daysAfterEnd"/>d].</summary>
  public static bool Contains(Season season, DateOnly date, int daysBeforeStart = Days, int daysAfterEnd = Days)
  {
    if (season.StartDate is { } start && date < start.AddDays(-daysBeforeStart))
    {
      return false;
    }

    if (season.EndDate is { } end && date > end.AddDays(daysAfterEnd))
    {
      return false;
    }

    return true;
  }
}
