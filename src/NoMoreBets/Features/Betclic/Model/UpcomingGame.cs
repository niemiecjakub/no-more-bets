namespace NoMoreBets.Features.Betclic.Model;

/// <summary>
/// Represents an upcoming game/match from Betclic Premier League page.
/// </summary>
public record UpcomingGame
{
    public required string Date { get; init; }
    public required string HomeTeam { get; init; }
    public required string AwayTeam { get; init; }
    public required string Time { get; init; }
    public double? HomeOdds { get; init; }
    public double? DrawOdds { get; init; }
    public double? AwayOdds { get; init; }
    public required string Url { get; init; }
}
