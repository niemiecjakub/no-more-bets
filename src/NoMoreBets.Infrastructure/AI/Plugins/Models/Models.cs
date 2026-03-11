using System.ComponentModel;

namespace NoMoreBets.Infrastructure.AI.Plugins.Models;


public record TeamLineupResult(string LineupType, IReadOnlyList<Player> Players);

public record Player(string Name, string Position);

public record MatchLineupResult(TeamLineupResult Home, TeamLineupResult Away);

public record TeamInjuriesResult(IReadOnlyList<InjuriedPlayer> Injuries);

public record InjuriedPlayer(string Name, string Position, string InjuryStatus) : Player(Name, Position);

public record MatchInjuriesResult(TeamInjuriesResult Home, TeamInjuriesResult Away);

public record RecentMatch(int MatchId, string Opponent, string Score, string Result, DateOnly Date);

public record PlayerRecentRatings(
  [Description("Player name")] string Player,
  [Description("Recent match ratings sorted by date (oldest first)")] IReadOnlyList<double> RecentRatings,
  [Description("Average of recent ratings")] double AvgRating);

public record TeamPerformanceResult(
  [Description("Top players by average rating with their recent ratings")] IReadOnlyList<PlayerRecentRatings> TopPlayers,
  [Description("Team rating in each of the recent matches, sorted by date (oldest first)")] IReadOnlyList<double> RecentTeamRatings,
  [Description("Average team rating across recent matches")] double AvgTeamRating,
  [Description("Formation used in each recent match, sorted by date")] IReadOnlyList<string> Formations);

public record MarketPriceHistory(
    [Description("The unique identifier for the market type")] string MarketKey,
    [Description("The human-readable name of the market, e.g., 'Total Goals'")] string? MarketDisplayName,
    IReadOnlyList<OutcomePriceTimeline> Outcomes
);

public record OutcomePriceTimeline(
    [Description("The name of the specific outcome")] string OutcomeName,
    [Description("The historical progression of prices, sorted by date")] IReadOnlyList<PricePoint> Timeline
);

public record PricePoint(
    [Description("The decimal odds value")] double Price,
    [Description("The timestamp when this price became live")] DateTime EffectiveFrom,
    [Description("The timestamp when this price changed or the market closed. Null if current.")] DateTime? EffectiveTo
);

internal sealed class EventTypeOddsAccumulator
{
  public string EventTypeName { get; set; } = "";
  public string? Title { get; set; }
  public List<string> OptionOrder { get; set; } = new();
  public Dictionary<string, List<(double Odds, DateTime At)>> OddsByLabel { get; set; } = new(StringComparer.Ordinal);
}
