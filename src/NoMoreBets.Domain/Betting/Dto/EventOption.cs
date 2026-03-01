namespace NoMoreBets.Domain.Betting.Dto;

/// <summary>
/// Represents a single betting option within a bookmaker event.
/// </summary>
public record EventOption
{
    public required string Label { get; init; }
    public required double Odds { get; init; }
}
