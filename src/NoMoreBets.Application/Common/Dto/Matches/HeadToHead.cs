namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Head-to-head data between two teams from SoccerData API.</summary>
public record HeadToHead
{
    public TeamInfo Team1 { get; init; } = null!;
    public TeamInfo Team2 { get; init; } = null!;
    public HeadToHeadStats Stats { get; init; } = null!;
}
