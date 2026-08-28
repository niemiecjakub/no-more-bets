namespace NoMoreBets.Domain.Betting;

public class BetRiskLevelEntity
{
  public const string TABLE_NAME = "BetRiskLevel";
  public int Id { get; set; }
  public string Name { get; set; } = null!;
}
