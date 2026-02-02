namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Injury entry for a player (position, player name, status).</summary>
public record InjuryInfo
{
    public required string Position { get; init; }
    public required string Player { get; init; }
    public required string Status { get; init; }
}
