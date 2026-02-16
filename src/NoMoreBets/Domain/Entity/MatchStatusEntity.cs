namespace NoMoreBets.Domain.Entity;

public class MatchStatusEntity
{
  public const string TABLE_NAME = "MatchStatus";
  public int Id { get; set; }
  public string Name { get; set; } = null!;
}
