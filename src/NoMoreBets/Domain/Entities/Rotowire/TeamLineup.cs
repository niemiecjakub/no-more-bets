namespace NoMoreBets.Domain.Entities.Rotowire;

/// <summary>
/// Represents a team's lineup and related information (e.g. Predicted Lineup, Confirmed Lineup).
/// </summary>
public record TeamLineup
{
    public required string TeamName { get; init; }
    public string? TeamCode { get; init; }
    public required string LineupType { get; init; }
    public IReadOnlyList<PlayerInLineup> Players { get; init; } = Array.Empty<PlayerInLineup>();
    public IReadOnlyList<InjuryEntry> Injuries { get; init; } = Array.Empty<InjuryEntry>();
}
