using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Common.Dto.Leagues;

/// <summary>Club overview from FotMob team page (recent games and daily summary).</summary>
public class ClubOverview
{
  public required IReadOnlyList<ClubRecentGame> RecentGames { get; init; }
  public required string DailySummary { get; init; }
}

public class ClubRecentGame
{
  public int OpponentId { get; init; }
  public string Score { get; init; } = "";
  public MatchResult Result { get; init; }
  public string GameUrl { get; init; } = "";
}
