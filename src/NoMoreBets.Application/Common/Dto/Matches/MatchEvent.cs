namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Match event (goal, card, substitution, etc.).</summary>
public record MatchEvent
{
    public string EventType { get; init; } = string.Empty;
    public string EventMinute { get; init; } = string.Empty;
    public string Team { get; init; } = string.Empty;
    public Player? Player { get; init; }
    public Player? AssistPlayer { get; init; }
    public Player? PlayerIn { get; init; }
    public Player? PlayerOut { get; init; }
}
