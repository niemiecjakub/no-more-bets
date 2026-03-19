using System.ComponentModel;

namespace NoMoreBets.Application.Clubs.GetClubRollingPerformance;

public record PlayerRecentRatings(
  [Description("Player name")] string Player,
  [Description("Recent match ratings sorted by date (oldest first)")] IReadOnlyList<double> RecentRatings,
  [Description("Average of recent ratings")] double AvgRating);

public record TeamPerformanceResult(
  [Description("Top players by average rating with their recent ratings")] IReadOnlyList<PlayerRecentRatings> TopPlayers,
  [Description("Team rating in each of the recent matches, sorted by date (oldest first)")] IReadOnlyList<double> RecentTeamRatings,
  [Description("Average team rating across recent matches")] double AvgTeamRating,
  [Description("Formation used in each recent match, sorted by date")] IReadOnlyList<string> Formations);
