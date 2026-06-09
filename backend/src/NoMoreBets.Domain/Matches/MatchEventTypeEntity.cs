namespace NoMoreBets.Domain.Matches;

public class MatchEventTypeEntity
{
  public const string TABLE_NAME = "MatchEventType";
  public int Id { get; set; }
  public string Name { get; set; } = null!;
}
