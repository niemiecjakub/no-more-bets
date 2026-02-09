namespace NoMoreBets.Features.Fotmob.GetFotmobMatchDetails.Dtos;

using NoMoreBets.Features.Fotmob.Model;

/// <summary>API response DTO for a stat group (e.g. Possession, Shots).</summary>
public record StatGroupDto(string Title, IReadOnlyList<StatRowDto> Rows)
{
    public static StatGroupDto From(StatGroup source) =>
        new(source.Title, source.Rows.Select(StatRowDto.From).ToList());
}
