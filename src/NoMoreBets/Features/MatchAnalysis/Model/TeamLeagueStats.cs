namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Team league/standings statistics (subset of FotMob Club data).</summary>
public record TeamLeagueStats
{
    public int Position { get; init; }
    public required string TeamName { get; init; }
    public required string TeamShortname { get; init; }
    public int TeamId { get; init; }
    public required string TeamLogoUrl { get; init; }
    public int MatchesPlayed { get; init; }
    public int Wins { get; init; }
    public int Draws { get; init; }
    public int Losses { get; init; }
    public int GoalsFor { get; init; }
    public int GoalsAgainst { get; init; }
    public required string GoalDifference { get; init; }
    public int Points { get; init; }
    public required string Form { get; init; }
    public int? NextOpponentId { get; init; }
    public string? NextOpponentName { get; init; }
    public string? NextOpponentLogoUrl { get; init; }
}
