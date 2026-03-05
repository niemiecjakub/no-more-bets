namespace NoMoreBets.Application.Common.Dto.Leagues;

/// <summary>Match details from FotMob match detail page (general info, lineups, and optional statistics).</summary>
public class MatchDetailsDto
{
    public required string HomeTeam { get; init; }
    public required string AwayTeam { get; init; }
    public DateTimeOffset? MatchDate { get; init; }
    public TeamLineup? HomeLineup { get; init; }
    public TeamLineup? AwayLineup { get; init; }
    public IReadOnlyList<StatGroup>? Statistics { get; init; }
    public IReadOnlyList<PlayerMatchStats>? Players { get; init; }
}
