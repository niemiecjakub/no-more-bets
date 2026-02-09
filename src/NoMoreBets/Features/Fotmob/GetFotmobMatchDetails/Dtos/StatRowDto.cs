namespace NoMoreBets.Features.Fotmob.GetFotmobMatchDetails.Dtos;

using NoMoreBets.Features.Fotmob.Model;

/// <summary>API response DTO for a single stat row (label + home/away values).</summary>
public record StatRowDto(string Label, string? HomeValue, string? AwayValue)
{
    public static StatRowDto From(StatRow source) =>
        new(source.Label, source.HomeValue, source.AwayValue);
}
