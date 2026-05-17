using NoMoreBets.Domain.Leagues;
using System.ComponentModel;

namespace NoMoreBets.Domain.Clubs;

public record ClubLeagueStats
{
  [Description("Current rank in the league table")]
  public int Position { get; init; }

  [Description("Total points accumulated in the current season")]
  public int Points { get; init; }

  public int Wins { get; init; }
  public int Draws { get; init; }
  public int Losses { get; init; }

  [Description("Total goals scored")]
  public int GoalsFor { get; init; }

  [Description("Total goals conceded")]
  public int GoalsAgainst { get; init; }

  [Description("Expected Goals (xG): Quality of chances created")]
  public decimal Xg { get; init; }

  [Description("The variance between actual goals scored and xG")]
  public decimal XgDiff { get; init; }

  [Description("Expected Goals Against (xGA): Quality of chances conceded")]
  public decimal Xga { get; init; }

  [Description("The variance between actual goals conceded and xGA")]
  public decimal XgaDiff { get; init; }

  [Description("Expected Points (xPts): Points the team deserved based on performance quality")]
  public decimal Xpts { get; init; }

  [Description("Variance between actual points and xPts. Negative suggests the team is over-performing/lucky.")]
  public decimal XptsDiff { get; init; }

  public ClubLeagueStats(LeagueTableSnapshotRow rowSnapshot)
  {
    Position = rowSnapshot.Position;
    Points = rowSnapshot.Points;
    Wins = rowSnapshot.Wins;
    Draws = rowSnapshot.Draws;
    Losses = rowSnapshot.Losses;
    GoalsFor = rowSnapshot.GoalsFor;
    GoalsAgainst = rowSnapshot.GoalsAgainst;
    Xg = rowSnapshot.Xg;
    XgDiff = rowSnapshot.XgDiff;
    Xga = rowSnapshot.Xga;
    XgaDiff = rowSnapshot.XgaDiff;
    Xpts = rowSnapshot.Xpts;
    XptsDiff = rowSnapshot.XptsDiff;
  }
}
