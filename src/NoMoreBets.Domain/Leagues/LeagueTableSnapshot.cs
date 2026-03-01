namespace NoMoreBets.Domain.Leagues;

public class LeagueTableSnapshot
{
  public long Id { get; set; }
  public int LeagueId { get; set; }
  public int SeasonId { get; set; }
  public DateOnly SnapshotDate { get; set; }

  public League League { get; set; } = null!;
  public Season Season { get; set; } = null!;
  public ICollection<LeagueTableSnapshotRow> Rows { get; set; } = new List<LeagueTableSnapshotRow>();
}
