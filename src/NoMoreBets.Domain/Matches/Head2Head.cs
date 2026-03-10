using NoMoreBets.Domain.Clubs;
using System.ComponentModel;
using System.Globalization;

namespace NoMoreBets.Domain.Matches;

public class Head2Head
{
  public int Team1Id { get; set; }
  public int Team2Id { get; set; }
  public string Head2HeadJson { get; set; } = null!;
  public DateTime UpdatedAt { get; set; }

  public Club Team1 { get; set; } = null!;
  public Club Team2 { get; set; } = null!;

  public static (int Team1Id, int Team2Id) NormalizeClubIds(int club1Id, int club2Id)
  {
    return (Math.Min(club1Id, club2Id), Math.Max(club1Id, club2Id));
  }
}

public static class Head2HeadQueryableExtensions
{
  public static IQueryable<Head2Head> ForClubs(this IQueryable<Head2Head> query, int club1Id, int club2Id)
  {
    var (team1Id, team2Id) = Head2Head.NormalizeClubIds(club1Id, club2Id);
    return query.Where(h => h.Team1Id == team1Id && h.Team2Id == team2Id);
  }
}


//public record HeadToHead
//{
//  public TeamInfo Team1 { get; init; } = null!;
//  public TeamInfo Team2 { get; init; } = null!;

//  [Description("Aggregated historical statistics between these two clubs")]
//  public HeadToHeadStats Stats { get; init; } = null!;
//}

//public record HeadToHeadStats(
//    [Description("Total history across all venues")]
//    HistoricalSummary Overall,

//    [Description("Stats specifically when Team 1 played at their home stadium")]
//    HistoricalSummary Team1AtHome,

//    [Description("Stats specifically when Team 2 played at their home stadium")]
//    HistoricalSummary Team2AtHome
//);

//public record HistoricalSummary
//{
//  public int GamesPlayed { get; init; }

//  [Description("Number of matches won by the first team in this context")]
//  public int Wins { get; init; }

//  [Description("Number of matches won by the second team in this context")]
//  public int Losses { get; init; }

//  public int Draws { get; init; }

//  [Description("Goals scored by the first team")]
//  public int GoalsScored { get; init; }

//  [Description("Goals scored by the second team")]
//  public int GoalsConceded { get; init; }

//  // Calculated property to help the AI identify scoring trends
//  [Description("Average total goals per game in this context")]
//  public double AverageTotalGoals => GamesPlayed > 0
//      ? (double)(GoalsScored + GoalsConceded) / GamesPlayed
//      : 0;
//}