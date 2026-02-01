using NoMoreBets.Features.Betclic.Model;

namespace NoMoreBets.Features.Betclic.GetBetclicUpcomingGames.Dtos;

/// <summary>API response DTO for an upcoming game.</summary>
public record UpcomingGameDto(
    string Date,
    string HomeTeam,
    string AwayTeam,
    string Time,
    double? HomeOdds,
    double? DrawOdds,
    double? AwayOdds,
    string Url)
{
    public static UpcomingGameDto From(UpcomingGame source) =>
        new(
            source.Date,
            source.HomeTeam,
            source.AwayTeam,
            source.Time,
            source.HomeOdds,
            source.DrawOdds,
            source.AwayOdds,
            source.Url);
}
