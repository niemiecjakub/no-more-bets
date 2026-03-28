namespace NoMoreBets.Domain.Betting;

public class BettingEventOptionEntity
{
  public const string TABLE_NAME = "BettingEventOption";
  public int Id { get; set; }
  public string Name { get; set; } = null!;
}
