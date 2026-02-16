namespace NoMoreBets.Domain.Entity;

public class Stage
{
  public int Id { get; set; }
  public int SeasonId { get; set; }
  public int? SoccerdataId { get; set; }
  public string Name { get; set; } = null!;

  public Season Season { get; set; } = null!;
  public ICollection<Match> Matches { get; set; } = new List<Match>();
}
