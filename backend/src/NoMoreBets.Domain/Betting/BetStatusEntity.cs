namespace NoMoreBets.Domain.Betting;

public class BetStatusEntity
{
  public const string TABLE_NAME = "BetStatus";
  public int Id { get; set; }
  public string Name { get; set; } = null!;
}
