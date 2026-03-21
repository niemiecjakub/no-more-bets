using NoMoreBets.Domain.Clubs;

namespace NoMoreBets.Domain.Leagues;

public class League
{
  public int Id { get; set; }
  public string Name { get; set; } = null!;
  public string Slug { get; set; } = null!;
  public int SoccerdataId { get; set; }

  public ICollection<Club> Clubs { get; set; } = new List<Club>();
  public ICollection<Season> Seasons { get; set; } = new List<Season>();
  public ICollection<LeagueTableSnapshot> LeagueTableSnapshots { get; set; } = new List<LeagueTableSnapshot>();
}
