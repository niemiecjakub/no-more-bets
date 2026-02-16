namespace NoMoreBets.Domain.Entity;

public class Club
{
  public int Id { get; set; }
  public string Name { get; set; } = null!;
  public int LeagueId { get; set; }
  public int SoccerdataId { get; set; }

  public League League { get; set; } = null!;
  public ICollection<Match> HomeMatches { get; set; } = new List<Match>();
  public ICollection<Match> AwayMatches { get; set; } = new List<Match>();
}
