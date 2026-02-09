namespace NoMoreBets.Features.Fotmob.Model;

/// <summary>Per-player match statistics from FotMob Statistics tab (individual stats table).</summary>
public class PlayerMatchStats
{
    public required string Player { get; init; }
    public required string Score { get; init; }
    public required string MinutesPlayed { get; init; }
    public required string Goals { get; init; }
    public required string Assists { get; init; }
    public required string Xg { get; init; }
    public required string Xa { get; init; }
    public required string XgPlusXa { get; init; }
    public required string DefensiveContributions { get; init; }
}
