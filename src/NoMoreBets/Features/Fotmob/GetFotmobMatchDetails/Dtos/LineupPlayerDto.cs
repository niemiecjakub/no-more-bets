namespace NoMoreBets.Features.Fotmob.GetFotmobMatchDetails.Dtos;

using NoMoreBets.Features.Fotmob.Model;

/// <summary>API response DTO for a single player in a match lineup.</summary>
public record LineupPlayerDto(string Name, double? Rating)
{
    public static LineupPlayerDto From(LineupPlayer source) =>
        new(source.Name, source.Rating);
}
