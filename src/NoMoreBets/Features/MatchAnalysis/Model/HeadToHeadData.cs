namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Head-to-head statistics between two teams.</summary>
public record HeadToHeadData
{
    public required TeamInfo Team1 { get; init; }
    public required TeamInfo Team2 { get; init; }
    public required OverallStats Overall { get; init; }
    public required Team1AtHomeStats Team1AtHome { get; init; }
    public required Team2AtHomeStats Team2AtHome { get; init; }
}
