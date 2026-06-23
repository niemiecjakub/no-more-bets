using System.Globalization;

namespace NoMoreBets.Application.Common.SoccerData;

/// <summary>Parses SoccerData match date/time strings into UTC kickoff times used by the app.</summary>
public static class SoccerDataKickoffDateParser
{
  /// <summary>Soccerdata kickoff times are stored in UTC but run 2 hours behind our match calendar.</summary>
  public static readonly TimeSpan Offset = TimeSpan.FromHours(2);

  public static bool TryParse(string dateStr, string? timeStr, out DateTime kickoffUtc)
  {
    kickoffUtc = default;
    if (string.IsNullOrWhiteSpace(dateStr))
    {
      return false;
    }

    if (!DateTime.TryParseExact(
      $"{dateStr.Trim()} {timeStr?.Trim() ?? "00:00"}",
      new[] { "dd/MM/yyyy HH:mm", "dd/MM/yyyy H:mm", "dd/MM/yyyy" },
      CultureInfo.InvariantCulture,
      DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
      out var parsed))
    {
      return false;
    }

    kickoffUtc = DateTime.SpecifyKind(parsed.Add(Offset), DateTimeKind.Utc);
    return true;
  }
}
