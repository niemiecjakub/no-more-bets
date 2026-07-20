using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Domain.Leagues;

public class Season
{
  public int Id { get; set; }
  public int LeagueId { get; set; }
  public string Year { get; set; } = null!;
  public DateOnly? StartDate { get; set; }
  public DateOnly? EndDate { get; set; }
  public League League { get; set; } = null!;
  public ICollection<Stage> Stages { get; set; } = new List<Stage>();
  public ICollection<LeagueTableSnapshot> LeagueTableSnapshots { get; set; } = new List<LeagueTableSnapshot>();
}
