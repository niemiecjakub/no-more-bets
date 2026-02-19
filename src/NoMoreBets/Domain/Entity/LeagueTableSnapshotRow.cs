namespace NoMoreBets.Domain.Entity;

public class LeagueTableSnapshotRow
{
  public long SnapshotId { get; set; }
  public int ClubId { get; set; }

  public int Position { get; set; }
  public int MatchesPlayed { get; set; }
  public int Wins { get; set; }
  public int Draws { get; set; }
  public int Losses { get; set; }
  public int GoalsFor { get; set; }
  public int GoalsAgainst { get; set; }
  public int GoalDifference { get; set; }
  public int Points { get; set; }

  public decimal Xg { get; set; }
  public decimal XgDiff { get; set; }
  public decimal Xga { get; set; }
  public decimal XgaDiff { get; set; }
  public decimal Xpts { get; set; }
  public decimal XptsDiff { get; set; }

  public LeagueTableSnapshot Snapshot { get; set; } = null!;
  public Club Club { get; set; } = null!;
}
