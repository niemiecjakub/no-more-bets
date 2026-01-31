namespace NoMoreBets.Domain.Entities.Rotowire;

/// <summary>
/// Represents a complete game with lineups, odds, and weather.
/// </summary>
public record GameLineup
{
    public required string Date { get; init; }
    public string? Time { get; init; }
    public required TeamLineup HomeTeam { get; init; }
    public required TeamLineup AwayTeam { get; init; }
    public GameOdds? Odds { get; init; }
    public WeatherInfo? Weather { get; init; }
}
