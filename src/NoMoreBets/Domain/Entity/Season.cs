namespace NoMoreBets.Domain.Entity;

public class Season
{
  public int Id { get; set; }
  public int LeagueId { get; set; }
  public string Year { get; set; } = null!;
  public League League { get; set; } = null!;
  public ICollection<Stage> Stages { get; set; } = new List<Stage>();
  public ICollection<LeagueTableSnapshot> LeagueTableSnapshots { get; set; } = new List<LeagueTableSnapshot>();
}
