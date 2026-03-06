namespace NoMoreBets.Application.Common.Dto.Leagues;

using NoMoreBets.Domain.Matches;

/// <summary>Match details from FotMob match detail page (general info, lineups, and optional statistics).</summary>
public class MatchDetailsDto
{
    public required string HomeTeam { get; init; }
    public required string AwayTeam { get; init; }
    public DateTimeOffset? MatchDate { get; init; }
    public int? HomeScore { get; init; }
    public int? AwayScore { get; init; }
    public FotmobTeamLineup? HomeLineup { get; init; }
    public FotmobTeamLineup? AwayLineup { get; init; }
    public IReadOnlyList<FotmobStatGroup>? Statistics { get; init; }
    public IReadOnlyList<FotmobPlayerMatchStats>? Players { get; init; }
}
