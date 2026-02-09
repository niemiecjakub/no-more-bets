namespace NoMoreBets.Features.Fotmob.GetFotmobMatchDetails.Dtos;

using NoMoreBets.Features.Fotmob.Model;

/// <summary>API response DTO for per-player match statistics.</summary>
public record PlayerMatchStatsDto(
    string Player,
    string Score,
    string MinutesPlayed,
    string Goals,
    string Assists,
    string Xg,
    string Xa,
    string XgPlusXa,
    string DefensiveContributions)
{
    public static PlayerMatchStatsDto From(PlayerMatchStats source) =>
        new(
            source.Player,
            source.Score,
            source.MinutesPlayed,
            source.Goals,
            source.Assists,
            source.Xg,
            source.Xa,
            source.XgPlusXa,
            source.DefensiveContributions);
}
