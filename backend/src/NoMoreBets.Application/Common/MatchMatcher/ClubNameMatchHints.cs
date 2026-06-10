using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NoMoreBets.Application.Common.MatchMatcher;

/// <summary>
/// Deterministic name tweaks for matching external sources (e.g. Betclic PL) to seeded <see cref="NoMoreBets.Domain.Clubs.Club.Name"/> values.
/// </summary>
internal static class ClubNameMatchHints
{
  private static readonly Regex CollapseWhitespace = new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

  private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
  {
    ["Marsylia"] = "Marseille",
    ["FC Koeln"] = "FC Cologne",
    // German sources use "Köln"; DB uses English "FC Cologne". Folded keys cover umlauts via ResolveEffectiveName.
    ["1. FC Koln"] = "FC Cologne",
    ["FC Koln"] = "FC Cologne",
    ["1. FC Köln"] = "FC Cologne",
    ["Mönchengladbach"] = "Borussia M'gladbach",
    ["Borussia Monchengladbach"] = "Borussia M'gladbach",
    ["1899 Hoffenheim"] = "Hoffenheim",
    ["VfL Wolfsburg"] = "Wolfsburg",
    ["Hamburger SV"] = "Hamburg",
    // Common Spanish naming variants/abbreviations used by providers.
    ["Oviedo"] = "Real Oviedo",
    ["Real Sociedad de Futbol"] = "Real Sociedad",
    ["Real Sociedad de Fútbol"] = "Real Sociedad",
    ["Rayo Vallecano de Madrid"] = "Rayo Vallecano",
    ["CA Osasuna"] = "Osasuna",
    ["Sevilla FC"] = "Sevilla",
    ["Villarreal CF"] = "Villarreal",
    ["RC Celta de Vigo"] = "Celta Vigo",
    ["RC Celta"] = "Celta Vigo",
    ["RCD Espanyol"] = "Espanyol",
    ["Levante UD"] = "Levante",
    // Latin "Larnaca" vs transliterated Greek "Larnaka" for the same club (bookmakers vs Soccerdata/FotMob).
    ["AEK Larnaca"] = "AEK Larnaka",
    // Ligue 1 providers often use full PSG name while DB seed stores short form.
    ["Paris Saint-Germain"] = "PSG",
    ["Paris Saint Germain"] = "PSG",
    ["Paris SG"] = "PSG",
    ["Strasbourg"] = "RC Strasbourg",
    // Ekstraklasa/LaLiga localized variants seen in bookmaker feeds.
    ["Wisła Płock"] = "Wisla Plock",
    ["RKS Radomiak"] = "Radomiak Radom",
    ["Real Madryt"] = "Real Madrid",
    ["Bayern Monachium"] = "Bayern Munich",
    // FIFA World Cup: FotMob names vs Soccerdata/DB seed (004.sql).
    ["Bosnia and Herzegovina"] = "Bosnia-Herzegovina",
    ["Cape Verde"] = "Cabo Verde",
    ["DR Congo"] = "Congo DR",
    ["Ivory Coast"] = "Cote d'Ivoire",
    ["Iran"] = "IR Iran",
    ["South Korea"] = "Korea Republic",
    ["USA"] = "United States",
  };

  public static string ResolveEffectiveName(string trimmed)
  {
    if (!string.IsNullOrEmpty(trimmed))
    {
      trimmed = CollapseWhitespace.Replace(trimmed.Trim(), " ");
    }

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
