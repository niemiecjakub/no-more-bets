using NoMoreBets.Features.Fotmob.Model;

namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Team league/standings statistics (subset of FotMob Club data).</summary>
public record TeamLeagueStats
{
    public int CurrentPostition { get; init; }
    public int MatchesPlayed { get; init; }
    public int Wins { get; init; }
    public int Draws { get; init; }
    public int Losses { get; init; }
    public int GoalsFor { get; init; }
    public int GoalsAgainst { get; init; }
    public required string GoalDifference { get; init; }
    public int Points { get; init; }
    public IReadOnlyList<MatchResult> Form { get; init; } = Array.Empty<MatchResult>();
}
