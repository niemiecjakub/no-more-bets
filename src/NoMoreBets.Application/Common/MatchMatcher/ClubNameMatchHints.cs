using System.Globalization;
using System.Text;

namespace NoMoreBets.Application.Common.MatchMatcher;

/// <summary>
/// Deterministic name tweaks for matching external sources (e.g. Betclic PL) to seeded <see cref="NoMoreBets.Domain.Clubs.Club.Name"/> values.
/// </summary>
internal static class ClubNameMatchHints
{
  private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
  {
    ["Marsylia"] = "Marseille",
    ["FC Koeln"] = "FC Cologne",
    // German sources use "Köln"; DB uses English "FC Cologne". Folded keys cover umlauts via ResolveEffectiveName.
    ["1. FC Koln"] = "FC Cologne",
    ["FC Koln"] = "FC Cologne",
    // Latin "Larnaca" vs transliterated Greek "Larnaka" for the same club (bookmakers vs Soccerdata/FotMob).
    ["AEK Larnaca"] = "AEK Larnaka",
  };

  public static string ResolveEffectiveName(string trimmed)
  {
    if (Aliases.TryGetValue(trimmed, out var canonical))
    {
      return canonical;
    }

    var folded = FoldDiacritics(trimmed);
    if (Aliases.TryGetValue(folded, out canonical))
    {
      return canonical;
    }

    return trimmed;
  }

  public static string FoldDiacritics(string value)
  {
    if (string.IsNullOrEmpty(value))
    {
      return value;
    }

    var normalized = value.Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder(normalized.Length);
    foreach (var c in normalized)
    {
      if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
      {
        sb.Append(c);
      }
    }

    return sb.ToString().Normalize(NormalizationForm.FormC);
  }
}
