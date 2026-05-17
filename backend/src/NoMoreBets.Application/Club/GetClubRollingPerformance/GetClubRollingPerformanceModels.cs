using System.ComponentModel;

namespace NoMoreBets.Application.Clubs.GetClubRollingPerformance;

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
