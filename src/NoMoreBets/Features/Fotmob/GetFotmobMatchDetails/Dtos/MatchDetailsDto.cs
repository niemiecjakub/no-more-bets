namespace NoMoreBets.Features.Fotmob.GetFotmobMatchDetails.Dtos;

using NoMoreBets.Features.Fotmob.Model;

/// <summary>API response DTO for match details (general info, lineups, and optional statistics).</summary>
public record MatchDetailsDto(
    string HomeTeam,
    string AwayTeam,
    DateTimeOffset? MatchDate,
    TeamLineupDto? HomeLineup,
    TeamLineupDto? AwayLineup,
    IReadOnlyList<StatGroupDto>? Statistics,
    IReadOnlyList<PlayerMatchStatsDto>? Players)
{
    public static MatchDetailsDto From(MatchDetails source) =>
        new(
            source.HomeTeam,
            source.AwayTeam,
            source.MatchDate,
            source.HomeLineup is not null ? TeamLineupDto.From(source.HomeLineup) : null,
            source.AwayLineup is not null ? TeamLineupDto.From(source.AwayLineup) : null,
            source.Statistics?.Select(StatGroupDto.From).ToList(),
            source.Players?.Select(PlayerMatchStatsDto.From).ToList());
}
