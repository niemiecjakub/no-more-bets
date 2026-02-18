namespace NoMoreBets.Domain.Entity;

public class LeagueTableSnapshotRow
{
  public long SnapshotId { get; set; }
  public int ClubId { get; set; }

  public short Position { get; set; }
  public short MatchesPlayed { get; set; }
  public short Wins { get; set; }
  public short Draws { get; set; }
  public short Losses { get; set; }
  public short GoalsFor { get; set; }
  public short GoalsAgainst { get; set; }
  public short GoalDifference { get; set; }
  public short Points { get; set; }

  public decimal Xg { get; set; }
  public decimal XgDiff { get; set; }
  public decimal Xga { get; set; }
  public decimal XgaDiff { get; set; }
  public decimal Xpts { get; set; }
  public decimal XptsDiff { get; set; }

  public LeagueTableSnapshot Snapshot { get; set; } = null!;
  public Club Club { get; set; } = null!;
}
