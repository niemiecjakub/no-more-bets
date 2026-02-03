using FuzzySharp;
using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable.Dtos;
using NoMoreBets.Features.Rotowire.Model;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.MatchAnalysis.MatchMatcher;

/// <summary>
/// Matches teams and data across sources using exact key match first, then FuzzySharp.
/// </summary>
public sealed class MatchMatcher : IMatchMatcher
{
  private const int LineupAndSoccerDataScoreCutoff = 85;
  private const int FotmobScoreCutoff = 70;
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
      var key = new TeamKey(lineup.HomeTeam.TeamName, lineup.AwayTeam.TeamName);
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

    foreach (var club in clubs)
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
}
