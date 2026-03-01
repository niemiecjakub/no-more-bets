namespace NoMoreBets.Domain.Leagues.Dto;

/// <summary>Club overview from FotMob team page (recent games and daily summary).</summary>
public class ClubOverview
{
    public required IReadOnlyList<ClubRecentGame> RecentGames { get; init; }
    public required IReadOnlyList<string> DailySummary { get; init; }
}
