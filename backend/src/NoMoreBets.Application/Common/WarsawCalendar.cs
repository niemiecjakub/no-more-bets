namespace NoMoreBets.Application.Common;

public static class WarsawCalendar
{
  public static readonly TimeZoneInfo TimeZone = Resolve();

  private static TimeZoneInfo Resolve()
  {
    if (TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Warsaw", out var iana))
    {
      return iana;
    }

    if (TimeZoneInfo.TryFindSystemTimeZoneById("Central European Standard Time", out var windows))
    {
      return windows;
    }

    throw new InvalidOperationException("Could not resolve Europe/Warsaw.");
  }

  public static DateOnly DateFromUtc(DateTime utc)
  {
    var instant = UtcDateTime.ToUtc(utc);
    var local = TimeZoneInfo.ConvertTimeFromUtc(instant, TimeZone);
    return DateOnly.FromDateTime(local);
  }

  public static (DateTime StartUtc, DateTime EndUtcExclusive) UtcRangeForDate(DateOnly date)
  {
    var startLocal = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
    var endLocal = DateTime.SpecifyKind(date.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
    return (TimeZoneInfo.ConvertTimeToUtc(startLocal, TimeZone), TimeZoneInfo.ConvertTimeToUtc(endLocal, TimeZone));
  }
}
