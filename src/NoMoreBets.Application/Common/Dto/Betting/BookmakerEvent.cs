namespace NoMoreBets.Application.Common.Dto.Betting;

/// <summary>
/// Represents a bookmaker market/event with its betting options.
/// </summary>
public record BookmakerEvent
{
    public required string Title { get; init; }
    public required IReadOnlyList<EventOption> Options { get; init; }
}
