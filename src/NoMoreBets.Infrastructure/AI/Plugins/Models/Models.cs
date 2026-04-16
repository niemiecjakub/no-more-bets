using System.ComponentModel;

namespace NoMoreBets.Infrastructure.AI.Plugins.Models;


public record TeamLineupResult(string LineupType, IReadOnlyList<Player> Players);

public record Player(string Name, string Position);

public record MatchLineupResult(TeamLineupResult Home, TeamLineupResult Away);

public record AgentTeamLineup(IReadOnlyList<Player> Players);

public record AgentMatchLineup(AgentTeamLineup Home, AgentTeamLineup Away);

public record TeamInjuriesResult(IReadOnlyList<InjuriedPlayer> Injuries);

public record InjuriedPlayer(string Name, string Position, string InjuryStatus) : Player(Name, Position);

public record MatchInjuriesResult(TeamInjuriesResult Home, TeamInjuriesResult Away);

public record RecentMatch(int MatchId, string Opponent, string Score, string Result, DateOnly Date);

public record PlayerRecentRatings(
  [Description("Player name")] string Player,
  [Description("Recent match ratings sorted by date (oldest first)")] IReadOnlyList<double> RecentRatings,
  [Description("Average of recent ratings")] double AvgRating);

public record PlayerMatchRating(
  [Description("Player name")] string Player,
  [Description("Player rating from this match")] double Rating);

public record TeamPerformanceMatchStats(
  [Description("Match id")] int MatchId,
  [Description("Opponent club name")] string Opponent,
  [Description("Match date")] DateOnly Date,
  [Description("Team rating for this match")] double? TeamRating,
  [Description("Formation in this match")] string Formation,
  [Description("Rated players from this match")] IReadOnlyList<PlayerMatchRating> PlayerRatings);

public record TeamPerformanceResult(
  [Description("Top players by average rating with their recent ratings")] IReadOnlyList<PlayerRecentRatings> TopPlayers,
  [Description("Team rating in each of the recent matches, sorted by date (oldest first)")] IReadOnlyList<double> RecentTeamRatings,
  [Description("Average team rating across recent matches")] double AvgTeamRating,
  [Description("Formation used in each recent match, sorted by date")] IReadOnlyList<string> Formations,
  [Description("Per-match stats that were used to calculate averages and top players")] IReadOnlyList<TeamPerformanceMatchStats> Matches);

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
    [Description("The timestamp when this price became live")] DateTime Timestamp
);

public record H2H
{
  public string Summary { get; init; } = null!; // e.g., "Arsenal vs Liverpool"
  public int TotalMatches { get; init; }
  public int TotalDraws { get; init; }

  public TeamMetrics TeamA { get; init; } = null!;
  public TeamMetrics TeamB { get; init; } = null!;
}

public record TeamMetrics
{
  public string Name { get; init; } = null!;

  // Aggregates
  public int TotalWins { get; init; }
  public int TotalGoalsScored { get; init; }
  public int TotalGoalsConceded { get; init; }

  // Venue Breakdown
  public int HomeWins { get; init; }
  public int AwayWins { get; init; }

  public double WinPercentage { get; init; }
  public double AvgGoalsScored { get; init; }
  public double AvgGoalsConceded { get; init; }
}

internal sealed class EventTypeOddsAccumulator
{
  public string EventTypeName { get; set; } = "";
  public string? Title { get; set; }
  public List<string> OptionOrder { get; set; } = new();
  public Dictionary<string, List<(double Odds, DateTime At)>> OddsByLabel { get; set; } = new(StringComparer.Ordinal);
}


public record CurrentOddsMarket(int EventTypeId, string EventTypeName, string Title, IReadOnlyList<CurrentOddsOption> Options);

public record CurrentOddsOption(string Label, double Odds);

[Description("Match available for betting: use Id when calling GetCurrentOdds and GetMatchAnalysis")]
public record AvailableMatch(int Id, string HomeClubName, string AwayClubName, DateTime MatchDate);

