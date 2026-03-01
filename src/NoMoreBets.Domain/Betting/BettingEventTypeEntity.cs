namespace NoMoreBets.Domain.Betting;

public class BettingEventTypeEntity
{
  public const string TABLE_NAME = "BettingEventType";
  public int Id { get; set; }
  public string Name { get; set; } = null!;
}
