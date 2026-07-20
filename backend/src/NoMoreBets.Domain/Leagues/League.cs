namespace NoMoreBets.Domain.Leagues;

public class League
{
  public const string FifaWorldCupSlug = "fifa-world-cup";
  public const string UnknownSlug = "unknown";
  public const int UnknownSoccerdataId = 0;

  public int Id { get; set; }
  public string Name { get; set; } = null!;
  public string Slug { get; set; } = null!;
  public int SoccerdataId { get; set; }

  public ICollection<Season> Seasons { get; set; } = new List<Season>();
  public ICollection<LeagueTableSnapshot> LeagueTableSnapshots { get; set; } = new List<LeagueTableSnapshot>();
}
