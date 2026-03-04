namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Overall head-to-head statistics between two teams.</summary>
public record OverallStats
{
    public int OverallGamesPlayed { get; init; }
    public int OverallTeam1Wins { get; init; }
    public int OverallTeam2Wins { get; init; }
    public int OverallDraws { get; init; }
    public int OverallTeam1Scored { get; init; }
    public int OverallTeam2Scored { get; init; }
}
