namespace NoMoreBets.Application.Common;

public static class SeasonYearQueryExtensions
{
  public static string[] Normalize(string[]? seasonYears) =>
    (seasonYears ?? [])
      .Where(y => !string.IsNullOrWhiteSpace(y))
      .Select(y => y.Trim())
      .Distinct(StringComparer.Ordinal)
      .ToArray();
}
