namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Team league stats and recent games (FotMob-sourced).</summary>
public record FbrefTeamData
{
    public TeamLeagueStats? ClubStats { get; init; }
    public IReadOnlyList<RecentGameResult> RecentGames { get; init; } = [];
}
