using System.Text.RegularExpressions;
using FuzzySharp;
using Microsoft.Extensions.Logging;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Application.Common.Dto.Clubs;
using NoMoreBets.Application.Common.Dto.Leagues;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Application.Common.Dto.Matches;

namespace NoMoreBets.Application.Common.MatchMatcher;

/// <summary>
/// Matches teams and data across sources using exact key match first, then FuzzySharp.
/// </summary>
public sealed class MatchMatcher : IMatchMatcher
{
  private const int LineupAndSoccerDataScoreCutoff = 85;
  private const int FotmobScoreCutoff = 70;

  /// <summary>Ligue 1: external strings like "Paris FC" must not fuzzy-match to PSG.</summary>
  private static readonly Regex ParisFcInInput = new(@"\bParis\s+FC\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

  /// <summary>Ligue 1: PSG variants that must not fuzzy-match to Paris FC.</summary>
  private static readonly Regex PsgInInput = new(@"\bParis\s+Saint[- ]?Germain\b|\bParis\s+SG\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

  private readonly ILogger<MatchMatcher> _logger;

  public MatchMatcher(ILogger<MatchMatcher> logger)
  {
    _logger = logger;
  }

  /// <inheritdoc />
  public IReadOnlyDictionary<TeamKey, GameLineup> BuildLineupIndex(IReadOnlyList<GameLineup> lineups)
  {
    var dict = new Dictionary<TeamKey, GameLineup>(lineups.Count);
    foreach (var lineup in lineups)
    {
      var key = new TeamKey(lineup.HomeTeamName, lineup.AwayTeamName);
      dict[key] = lineup;
    }
    return dict;
  }

  /// <inheritdoc />
  public GameLineup? FindLineup(string home, string away, IReadOnlyDictionary<TeamKey, GameLineup> index)
  {
    var key = new TeamKey(home, away);
    if (index.TryGetValue(key, out var lineup))
    {
      return lineup;
    }

    var searchStr = key.ToSearchString();
    var candidates = index.ToDictionary(
        kv => kv.Key.ToSearchString(),
        kv => kv.Value,
        StringComparer.Ordinal);

    if (candidates.Count == 0)
    {
      _logger.LogError("No lineup candidates were found for {Home} vs {Away}", home, away);
      return null;
    }

    var keys = candidates.Keys.ToList();
    var best = Process.ExtractOne(searchStr, keys, s => s, cutoff: LineupAndSoccerDataScoreCutoff);
    if (best is null)
    {
      _logger.LogError("No matching lineup found for {Home} vs {Away}", home, away);
      return null;
    }

    var value = best.Value;
    if (value != null && best.Score >= LineupAndSoccerDataScoreCutoff && candidates.TryGetValue(value, out var found))
    {
      return found;
    }

    _logger.LogError("No matching lineup found for {Home} vs {Away}", home, away);
    return null;
  }

  /// <inheritdoc />
  public UpcomingMatchPreview? FindSoccerDataMatch(string home, string away, IReadOnlyList<LeagueMatchPreviews> leagues)
  {
    var key = new TeamKey(home, away);
    var searchStr = key.ToSearchString();

    foreach (var league in leagues)
    {
      foreach (var match in league.MatchPreviews)
      {
        var matchKey = new TeamKey(match.Teams.Home.Name, match.Teams.Away.Name);
        if (matchKey.Equals(key))
        {
          return match;
        }
      }
    }

    var candidates = new Dictionary<string, UpcomingMatchPreview>(StringComparer.Ordinal);
    foreach (var league in leagues)
    {
      foreach (var match in league.MatchPreviews)
      {
        var k = new TeamKey(match.Teams.Home.Name, match.Teams.Away.Name);
        candidates[k.ToSearchString()] = match;
      }
    }

    if (candidates.Count == 0)
    {
      _logger.LogError("No candidates were found for {Home} vs {Away}", home, away);
      return null;
    }
    var keys = candidates.Keys.ToList();
    var best = Process.ExtractOne(searchStr, keys, s => s, cutoff: LineupAndSoccerDataScoreCutoff);
    if (best == null)
    {
      _logger.LogError("No matching soccer data match found for {Home} vs {Away}. Bets was null", home, away);
      return null;
    }

    var value = best.Value;
    if (value != null && best.Score >= LineupAndSoccerDataScoreCutoff && candidates.TryGetValue(value, out var found))
    {
      return found;
    }

    _logger.LogError("No matching soccer data match found for {Home} vs {Away}", home, away);
    return null;
  }

  /// <inheritdoc />
  public ClubDto? FindFotmobClub(string teamName, IReadOnlyList<ClubDto> clubs)
  {
    if (clubs.Count == 0)
    {
      _logger.LogError("No clubs to perform search from");
      return null;
    }

    var normalized = (teamName ?? string.Empty).Trim().ToLowerInvariant();
    foreach (var club in clubs)
    {
      if (club.TeamName.Trim().ToLowerInvariant() == normalized)
      {
        return club;
      }
    }

    foreach (var club in clubs.OrderByDescending(c => (c.TeamName ?? string.Empty).Trim().Length))
    {
      var cn = club.TeamName.Trim().ToLowerInvariant();
      if (normalized.Contains(cn, StringComparison.Ordinal) || cn.Contains(normalized, StringComparison.Ordinal))
      {
        return club;
      }
    }

    var choices = clubs.Select(c => c.TeamName).ToArray();
    if (choices.Length == 0)
    {
      _logger.LogError("No club choices selected");
      return null;
    }

    var best = Process.ExtractOne(teamName, choices, s => s ?? "", cutoff: FotmobScoreCutoff);
    if (best == null)
    {
      _logger.LogError("No matching club data found for {Club}", teamName);
      return null;
    }
    var idx = best.Index;
    if (idx >= 0 && idx < clubs.Count && best.Score >= FotmobScoreCutoff)
    {
      return clubs[idx];
    }

    _logger.LogError("No matching club data found for {Club}", teamName);
    return null;
  }

  /// <inheritdoc />
  public Club FindClub(string teamName, IReadOnlyList<Club> clubs)
  {
    var trimmed = (teamName ?? string.Empty).Trim();
    if (clubs.Count == 0)
    {
      throw new InvalidOperationException(
        $"No clubs in league to match. Cannot resolve team '{trimmed}'.");
    }

    var effectiveName = ClubNameMatchHints.ResolveEffectiveName(trimmed);

    foreach (var club in clubs)
    {
      if (string.Equals(club.Name.Trim(), effectiveName, StringComparison.OrdinalIgnoreCase))
      {
        return club;
      }
    }

    var foldedEffective = ClubNameMatchHints.FoldDiacritics(effectiveName);
    foreach (var club in clubs)
    {
      var foldedClub = ClubNameMatchHints.FoldDiacritics(club.Name.Trim());
      if (string.Equals(foldedClub, foldedEffective, StringComparison.OrdinalIgnoreCase))
      {
        return club;
      }
    }

    var names = clubs.Select(c => ClubNameMatchHints.FoldDiacritics(c.Name)).ToArray();
    var best = Process.ExtractOne(foldedEffective, names, s => s ?? "", cutoff: FotmobScoreCutoff);
    if (best != null
      && best.Score >= FotmobScoreCutoff
      && best.Index >= 0
      && best.Index < clubs.Count
      && !ClubNameMatchHints.HasConflictingIdentityTokens(foldedEffective, names[best.Index]))
    {
      return DisambiguateParisClubs(trimmed, effectiveName, clubs[best.Index], clubs);
    }

    throw new ClubMatchNotFoundException(
      trimmed,
      $"No matching club found for '{trimmed}' among {clubs.Count} clubs. Ensure the team exists in the league.");
  }

  /// <summary>
  /// When both Paris FC and PSG exist in the same league list, FuzzySharp can pick the wrong Paris club.
  /// Prefer explicit "Paris FC" vs PSG naming (including <see cref="ClubNameMatchHints"/> canonical "PSG").
  /// </summary>
  private static Club DisambiguateParisClubs(string trimmed, string effectiveName, Club chosen, IReadOnlyList<Club> clubs)
  {
    Club? parisFc = null;
    Club? psg = null;
    foreach (var c in clubs)
    {
      var n = c.Name.Trim();
      if (n.Equals("Paris FC", StringComparison.OrdinalIgnoreCase))
      {
        parisFc = c;
      }
      else if (n.Equals("PSG", StringComparison.OrdinalIgnoreCase))
      {
        psg = c;
      }
    }

    if (parisFc is null || psg is null)
    {
      return chosen;
    }

    var meantPsg = PsgInInput.IsMatch(trimmed)
      || effectiveName.Trim().Equals("PSG", StringComparison.OrdinalIgnoreCase);
    var meantParisFc = ParisFcInInput.IsMatch(trimmed);

    if (ReferenceEquals(chosen, psg) && meantParisFc && !meantPsg)
    {
      return parisFc;
    }

    if (ReferenceEquals(chosen, parisFc) && meantPsg)
    {
      return psg;
    }

    return chosen;
  }

  /// <inheritdoc />
  public XgStats? FindXgStats(string teamName, IReadOnlyList<XgStats> xgStats)
  {
    if (xgStats.Count == 0)
    {
      _logger.LogError("No xG stats to perform search from");
      return null;
    }

    var normalized = (teamName ?? string.Empty).Trim().ToLowerInvariant();
    foreach (var stat in xgStats)
    {
      var tn = (stat.TeamName ?? string.Empty).Trim().ToLowerInvariant();
      if (tn == normalized)
      {
        return stat;
      }
    }

    foreach (var stat in xgStats.OrderByDescending(s => (s.TeamName ?? string.Empty).Trim().Length))
    {
      var tn = (stat.TeamName ?? string.Empty).Trim().ToLowerInvariant();
      if (normalized.Contains(tn, StringComparison.Ordinal) || tn.Contains(normalized, StringComparison.Ordinal))
      {
        return stat;
      }
    }

    var choices = xgStats.Select(s => s.TeamName ?? string.Empty).ToArray();
    if (choices.Length == 0)
    {
      _logger.LogError("No xG stats choices for matching");
      return null;
    }

    var best = Process.ExtractOne(teamName, choices, s => s ?? "", cutoff: FotmobScoreCutoff);
    if (best == null)
    {
      _logger.LogError("No matching xG stats found for {Club}", teamName);
      return null;
    }
    var idx = best.Index;
    if (idx >= 0 && idx < xgStats.Count && best.Score >= FotmobScoreCutoff)
    {
      return xgStats[idx];
    }

    _logger.LogError("No matching xG stats found for {Club}", teamName);
    return null;
  }

  /// <inheritdoc />
  public T? FindBestMatch<T>(string home, string away, IReadOnlyList<(string HomeName, string AwayName, T Value)> candidates)
  {
    if (candidates.Count == 0)
    {
      return default;
    }

    var key = new TeamKey(home, away);
    var searchStr = key.ToSearchString();

    foreach (var c in candidates)
    {
      var matchKey = new TeamKey(c.HomeName, c.AwayName);
      if (matchKey.Equals(key))
      {
        return c.Value;
      }
    }

    var dict = new Dictionary<string, T>(StringComparer.Ordinal);
    foreach (var c in candidates)
    {
      var k = new TeamKey(c.HomeName, c.AwayName);
      dict[k.ToSearchString()] = c.Value;
    }

    var keys = dict.Keys.ToList();
    var best = Process.ExtractOne(searchStr, keys, s => s, cutoff: LineupAndSoccerDataScoreCutoff);
    if (best is null)
    {
      return default;
    }

    var value = best.Value;
    if (value != null && best.Score >= LineupAndSoccerDataScoreCutoff && dict.TryGetValue(value, out var found))
    {
      return found;
    }

    return default;
  }
}
