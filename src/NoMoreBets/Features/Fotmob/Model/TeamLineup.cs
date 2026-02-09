namespace NoMoreBets.Features.Fotmob.Model;

/// <summary>Team lineup (formation, rating, players) from FotMob match detail page.</summary>
public class TeamLineup
{
    public required string TeamName { get; init; }
    public string? Formation { get; init; }
    public double? TeamRating { get; init; }
    public required IReadOnlyList<LineupPlayer> Players { get; init; }
}
