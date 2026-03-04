using NoMoreBets.Domain.Clubs;
using NoMoreBets.Application.Common.Dto.Clubs;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Application.Common.Dto.Matches;

namespace NoMoreBets.Application.Common.MatchMatcher;

/// <summary>
/// Matches teams and data across sources (Rotowire lineups, SoccerData previews, FotMob clubs).
/// Operates on feature types; handler maps results into MatchAnalysis models.
/// </summary>
public interface IMatchMatcher
{
  /// <summary>Builds an index of lineups by team key (order-independent home/away).</summary>
  IReadOnlyDictionary<TeamKey, GameLineup> BuildLineupIndex(IReadOnlyList<GameLineup> lineups);

  /// <summary>Finds a lineup matching the given match preview (exact or fuzzy).</summary>
  GameLineup? FindLineup(string home, string away, IReadOnlyDictionary<TeamKey, GameLineup> index);

  /// <summary>Finds an upcoming match preview by home/away team names (exact or fuzzy).</summary>
  UpcomingMatchPreview? FindSoccerDataMatch(string home, string away, IReadOnlyList<LeagueMatchPreviews> leagues);

  /// <summary>Finds a FotMob club (DTO) by team name (exact or fuzzy).</summary>
  ClubDto? FindFotmobClub(string teamName, IReadOnlyList<ClubDto> clubs);

  /// <summary>Finds a domain Club by team name (exact or fuzzy) from the given list. Throws if no match.</summary>
  Club FindClub(string teamName, IReadOnlyList<Club> clubs);

  /// <summary>Finds xG stats by team name (exact or fuzzy).</summary>
  XgStatsDto? FindXgStats(string teamName, IReadOnlyList<XgStatsDto> xgStats);

  /// <summary>Finds the best matching candidate by home/away team names (exact or fuzzy).</summary>
  T? FindBestMatch<T>(string home, string away, IReadOnlyList<(string HomeName, string AwayName, T Value)> candidates);
}
