namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Player in a lineup (position and name).</summary>
public record PlayerInLineupInfo
{
    public required string Position { get; init; }
    public required string Player { get; init; }
}
