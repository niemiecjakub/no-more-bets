namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Match goals at different stages.</summary>
public record Goals
{
    public int HomeHtGoals { get; init; }
    public int AwayHtGoals { get; init; }
    public int HomeFtGoals { get; init; }
    public int AwayFtGoals { get; init; }
    public int HomeEtGoals { get; init; }
    public int AwayEtGoals { get; init; }
    public int HomePenGoals { get; init; }
    public int AwayPenGoals { get; init; }
}
