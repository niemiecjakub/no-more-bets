namespace NoMoreBets.Features.Fotmob.GetFotmobMatchDetails.Dtos;

using NoMoreBets.Features.Fotmob.Model;

/// <summary>API response DTO for a team lineup (formation, rating, players).</summary>
public record TeamLineupDto(string TeamName, string? Formation, double? TeamRating, IReadOnlyList<LineupPlayerDto> Players)
{
    public static TeamLineupDto From(TeamLineup source) =>
        new(
            source.TeamName,
            source.Formation,
            source.TeamRating,
            source.Players.Select(LineupPlayerDto.From).ToList());
}
