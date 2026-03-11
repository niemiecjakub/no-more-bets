namespace NoMoreBets.Domain.Matches;

public class MatchAnalysis
{
  public int Id { get; set; }
  public int MatchId { get; set; }
  public string Code { get; set; } = null!;
  public string Content { get; set; } = null!;

  public Match Match { get; set; } = null!;
}
