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

  private static readonly Dictionary<string, string> Aliases = CreateAliases();

  private static Dictionary<string, string> CreateAliases()
  {
    var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
      ["Deportivo A Coruña"] = "Deportivo La Coruna",
      ["Deportivo A Coruna"] = "Deportivo La Coruna",
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
      // Flashscore Ekstraklasa short labels (flashscore-names.html)
      ["Jagiellonia"] = "Jagiellonia Bialystok",
      ["Legia"] = "Legia Warsaw",
      ["Rakow"] = "Rakow Czestochowa",
      ["Zaglebie"] = "Zaglebie Lubin",
      ["Wisla"] = "Wisla Krakow", // Flashscore uses full "Wisla Plock" for the other club
      ["Real Madryt"] = "Real Madrid",
      ["Bayern Monachium"] = "Bayern Munich",
      // Premier League promoted sides: Betclic uses short names; DB/FotMob use full club names.
      ["Coventry"] = "Coventry City",
      ["Hull"] = "Hull City",
      ["Ipswich"] = "Ipswich Town",
    };

    // FIFA World Cup (004.sql): Betclic PL, FotMob/Soccerdata, and RotoWire lineups.php naming variants.
    AddClubAlternatives(aliases, "Algeria", "Algieria");
    AddClubAlternatives(aliases, "Argentina", "Argentyna");
    AddClubAlternatives(aliases, "Belgium", "Belgia");
    AddClubAlternatives(aliases, "Bosnia-Herzegovina", "Bosnia and Herzegovina", "Bośnia i H.", "Bośnia i Hercegowina", "Bośnia i");
    AddClubAlternatives(aliases, "Brazil", "Brazylia");
    AddClubAlternatives(aliases, "Cabo Verde", "Cape Verde", "Wyspy Ziel. Przyl.","Wyspy Zielonego Przylądka");
    AddClubAlternatives(aliases, "Canada", "Kanada");
    AddClubAlternatives(aliases, "Colombia", "Kolumbia");
    AddClubAlternatives(aliases, "Congo DR", "DR Congo", "DR Konga");
    AddClubAlternatives(aliases, "Cote d'Ivoire", "Ivory Coast", "Wybrzeże Kości Słoniowej", "Cote D'ivoire");
    AddClubAlternatives(aliases, "Croatia", "Chorwacja");
    AddClubAlternatives(aliases, "Czechia", "Czechy", "Czech Republic");
    AddClubAlternatives(aliases, "Ecuador", "Ekwador");
    AddClubAlternatives(aliases, "Egypt", "Egipt");
    AddClubAlternatives(aliases, "England", "Anglia");
    AddClubAlternatives(aliases, "France", "Francja");
    AddClubAlternatives(aliases, "Germany", "Niemcy");
    AddClubAlternatives(aliases, "IR Iran", "Iran");
    AddClubAlternatives(aliases, "Iraq", "Irak");
    AddClubAlternatives(aliases, "Japan", "Japonia");
    AddClubAlternatives(aliases, "Jordan", "Jordania");
    AddClubAlternatives(aliases, "Korea Republic", "South Korea", "Korea Południowa");
    AddClubAlternatives(aliases, "Mexico", "Meksyk");
    AddClubAlternatives(aliases, "Morocco", "Maroko");
    AddClubAlternatives(aliases, "Netherlands", "Holandia");
    AddClubAlternatives(aliases, "New Zealand", "Nowa Zelandia");
    AddClubAlternatives(aliases, "Norway", "Norwegia");
    AddClubAlternatives(aliases, "Paraguay", "Paragwaj");
    AddClubAlternatives(aliases, "Portugal", "Portugalia");
    AddClubAlternatives(aliases, "Qatar", "Katar");
    AddClubAlternatives(aliases, "Saudi Arabia", "Arabia Saudyjska");
    AddClubAlternatives(aliases, "Scotland", "Szkocja");
    AddClubAlternatives(aliases, "South Africa", "RPA");
    AddClubAlternatives(aliases, "Spain", "Hiszpania");
    AddClubAlternatives(aliases, "Sweden", "Szwecja");
    AddClubAlternatives(aliases, "Switzerland", "Szwajcaria");
    AddClubAlternatives(aliases, "Tunisia", "Tunezja");
    AddClubAlternatives(aliases, "Turkiye", "Turcja", "Turkey");
    AddClubAlternatives(aliases, "United States", "USA");
    AddClubAlternatives(aliases, "Uruguay", "Urugwaj");

    return aliases;
  }

  private static void AddClubAlternatives(IDictionary<string, string> aliases, string canonical, params string[] alternatives)
  {
    foreach (var alternative in alternatives)
    {
      aliases[alternative] = canonical;
    }
  }

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
