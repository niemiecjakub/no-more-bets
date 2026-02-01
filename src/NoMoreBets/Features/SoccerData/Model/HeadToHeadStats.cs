namespace NoMoreBets.Features.SoccerData.Model;

/// <summary>All head-to-head statistics.</summary>
public record HeadToHeadStats
{
    public OverallStats Overall { get; init; } = null!;
    public Team1AtHomeStats Team1AtHome { get; init; } = null!;
    public Team2AtHomeStats Team2AtHome { get; init; } = null!;
}
