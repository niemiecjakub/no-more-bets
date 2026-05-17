using System.Globalization;
using System.Text.RegularExpressions;
using FuzzySharp;
using NoMoreBets.Application.Common.Dto.Betting;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using SoccerMatch = NoMoreBets.Domain.Matches.Match;

namespace NoMoreBets.Application.Common;

/// <summary>
/// Maps bookmaker option labels to <see cref="BettingEventOption"/> and builds
/// one <see cref="BettingOddsSnapshotRow"/> per option.
/// </summary>
public static class BookmakerEventOptionMapper
{
  /// <summary>Minimum fuzzy score (0–100) when comparing bookmaker labels to club names from <c>Match</c>.</summary>
  public const int ClubNameFuzzyScoreCutoff = 86;

  private static readonly Regex OverUnderRegex = new(
    @"^(?<dir>Powyżej|Poniżej)\s*(?<num>[\d,\.]+)\s*$",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

  private static readonly Regex ExactScoreRegex = new(
    @"^\s*(?<h>\d+)\s*-\s*(?<a>\d+)\s*$",
    RegexOptions.CultureInvariant);

  private static readonly Regex HandicapDrawRegex = new(
    @"^Remis\s*\(\s*(?<team>.+)\s+(?<sign>[+-])(?<n>\d+)\s*\)\s*$",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

  private static readonly Regex HandicapTeamRegex = new(
    @"^(?<team>.+)\s*\(\s*(?<sign>[+-])(?<n>\d+)\s*\)\s*$",
    RegexOptions.CultureInvariant);

  /// <summary>
  /// Builds snapshot rows for each scraped option (typed ids and odds only; no raw bookmaker JSON).
  /// </summary>
  public static IReadOnlyList<BettingOddsSnapshotRow> MapToRows(
    IReadOnlyList<EventOption> options,
    BettingEventType eventType,
    SoccerMatch match)
  {
    var rows = new List<BettingOddsSnapshotRow>(options.Count);
    foreach (var opt in options)
    {
      var mapped = MapOption(opt.Label, eventType, match);
      rows.Add(new BettingOddsSnapshotRow
      {
        EventType = eventType,
        EventOption = mapped,
        Odds = mapped.HasValue ? (decimal)opt.Odds : null
      });
    }

    return rows;
  }

  private static BettingEventOption? MapOption(
    string label,
    BettingEventType eventType,
    SoccerMatch match)
  {
    return eventType switch
    {
      BettingEventType.OverUnderGoals => MapOverUnderTotalGoals(label),
      BettingEventType.BothTeamsToScore => MapBtts(label),
      BettingEventType.MatchResult => MapMatchResult(label, match),
      BettingEventType.DoubleChance => MapDoubleChance(label, match),
      BettingEventType.Handicap => MapHandicap(label, match),
      BettingEventType.ExactScore => MapExactScore(label),
      _ => null
    };
  }

  /// <summary>Bookmaker labels often differ slightly from DB club names (suffixes, punctuation). Uses exact match then FuzzySharp.</summary>
  public static bool ClubNameMatches(string? bookmakerName, string? clubName)
  {
    if (string.IsNullOrWhiteSpace(bookmakerName) || string.IsNullOrWhiteSpace(clubName))
      return false;
    var a = ClubNameMatchHints.ResolveEffectiveName(bookmakerName.Trim());
    var b = ClubNameMatchHints.ResolveEffectiveName(clubName.Trim());
    if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
      return true;

    var af = ClubNameMatchHints.FoldDiacritics(a);
    var bf = ClubNameMatchHints.FoldDiacritics(b);
    if (string.Equals(af, bf, StringComparison.OrdinalIgnoreCase))
      return true;

    var aa = af.ToLowerInvariant();
    var bb = bf.ToLowerInvariant();
    var score = Math.Max(
      Math.Max(Fuzz.Ratio(aa, bb), Fuzz.PartialRatio(aa, bb)),
      Math.Max(Fuzz.TokenSortRatio(aa, bb), Fuzz.TokenSetRatio(aa, bb)));
    return score >= ClubNameFuzzyScoreCutoff;
  }

  private static BettingEventOption? MapOverUnderTotalGoals(string label)
  {
    var m = OverUnderRegex.Match(label.Trim());
    if (!m.Success)
      return null;

    var over = m.Groups["dir"].Value.Equals("Powyżej", StringComparison.OrdinalIgnoreCase);

    var suffix = ParseGoalLineSuffix(m.Groups["num"].Value);
    if (suffix is null)
      return null;

    return TryParseTotalGoalsEnum(over, suffix);
  }

  private static string? ParseGoalLineSuffix(string numRaw)
  {
    var s = numRaw.Trim();
    var sep = s.IndexOfAny([',', '.']);
    if (sep < 0)
      return null;
    var left = s[..sep].Trim();
    var right = s[(sep + 1)..].Trim();
    if (!int.TryParse(left, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole))
      return null;
    if (!int.TryParse(right, NumberStyles.Integer, CultureInfo.InvariantCulture, out var frac))
      return null;
    return $"{whole}_{frac}";
  }

  private static BettingEventOption? TryParseTotalGoalsEnum(bool over, string lineSuffix)
  {
    var ou = over ? "Over" : "Under";
    var name = $"TotalGoals_{ou}_{lineSuffix}";
    return Enum.TryParse(name, ignoreCase: false, out BettingEventOption option) ? option : null;
  }

  private static BettingEventOption? MapBtts(string label) => label.Trim() switch
  {
    var s when s.Equals("Tak", StringComparison.OrdinalIgnoreCase) => BettingEventOption.BothTeamsToScore_Yes,
    var s when s.Equals("Nie", StringComparison.OrdinalIgnoreCase) => BettingEventOption.BothTeamsToScore_No,
    _ => null
  };

  private static BettingEventOption? MapMatchResult(string label, SoccerMatch match)
  {
    var t = label.Trim();
    if (t.Equals("Remis", StringComparison.OrdinalIgnoreCase))
      return BettingEventOption.MatchResult_Draw;
    if (ClubNameMatches(t, match.HomeClub?.Name))
      return BettingEventOption.MatchResult_Home;
    if (ClubNameMatches(t, match.AwayClub?.Name))
      return BettingEventOption.MatchResult_Away;
    return null;
  }

  private static BettingEventOption? MapDoubleChance(string label, SoccerMatch match)
  {
    var home = match.HomeClub?.Name;
    var away = match.AwayClub?.Name;

    var t = label.Trim();
    const string homeDrawSuffix = " lub remis";
    if (t.EndsWith(homeDrawSuffix, StringComparison.OrdinalIgnoreCase))
    {
      var candidate = t[..^homeDrawSuffix.Length].Trim();
      if (ClubNameMatches(candidate, home))
        return BettingEventOption.DoubleChance_HomeOrDraw;
    }

    const string awayDrawPrefix = "remis lub ";
    if (t.StartsWith(awayDrawPrefix, StringComparison.OrdinalIgnoreCase))
    {
      var candidate = t[awayDrawPrefix.Length..].Trim();
      if (ClubNameMatches(candidate, away))
        return BettingEventOption.DoubleChance_AwayOrDraw;
    }

    const string sep = " lub ";
    var mid = t.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
    if (mid >= 0)
    {
      var left = t[..mid].Trim();
      var right = t[(mid + sep.Length)..].Trim();
      if (ClubNameMatches(left, home) && ClubNameMatches(right, away))
        return BettingEventOption.DoubleChance_HomeOrAway;
    }

    return null;
  }

  private static BettingEventOption? MapHandicap(string label, SoccerMatch match)
  {
    var home = match.HomeClub?.Name;
    var away = match.AwayClub?.Name;

    var drawMatch = HandicapDrawRegex.Match(label.Trim());
    if (drawMatch.Success)
    {
      var team = drawMatch.Groups["team"].Value.Trim();
      var sign = drawMatch.Groups["sign"].Value[0];
      if (!int.TryParse(drawMatch.Groups["n"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
        return null;
      if (!ClubNameMatches(team, home))
        return null;
      return TryParseHandicapOption("Draw", sign, n);
    }

    var teamMatch = HandicapTeamRegex.Match(label.Trim());
    if (!teamMatch.Success)
      return null;

    var teamName = teamMatch.Groups["team"].Value.Trim();
    var s = teamMatch.Groups["sign"].Value[0];
    if (!int.TryParse(teamMatch.Groups["n"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var line))
      return null;

    if (ClubNameMatches(teamName, home))
      return TryParseHandicapOption("Home", s, line);
    if (ClubNameMatches(teamName, away))
      return TryParseHandicapOption("Away", s, line);
    return null;
  }

  private static BettingEventOption? TryParseHandicapOption(string role, char sign, int n)
  {
    var signName = sign switch
    {
      '-' => "Minus",
      '+' => "Plus",
      _ => null
    };
    if (signName is null || n <= 0)
      return null;

    var name = $"Handicap_{role}_{signName}_{n}";
    return Enum.TryParse(name, ignoreCase: false, out BettingEventOption option) ? option : null;
  }

  private static BettingEventOption? MapExactScore(string label)
  {
    var t = label.Trim();
    if (t.Equals("Inny", StringComparison.OrdinalIgnoreCase))
      return BettingEventOption.CorrectScore_Other;

    var m = ExactScoreRegex.Match(t);
    if (!m.Success)
      return null;

    if (!int.TryParse(m.Groups["h"].Value, out var h) || !int.TryParse(m.Groups["a"].Value, out var a))
      return null;

    if (h < 0 || a < 0)
      return null;

    var name = $"CorrectScore_{h}_{a}";
    return Enum.TryParse(name, ignoreCase: false, out BettingEventOption option) ? option : null;
  }
}
