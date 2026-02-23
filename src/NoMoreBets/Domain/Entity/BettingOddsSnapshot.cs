namespace NoMoreBets.Domain.Entity;

public class BettingOddsSnapshot
{
  public long Id { get; set; }
  public int MatchId { get; set; }
  public DateTime SnapshotTime { get; set; }

  public Match Match { get; set; } = null!;
  public ICollection<BettingOddsSnapshotRow> Rows { get; set; } = new List<BettingOddsSnapshotRow>();
}
