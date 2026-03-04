namespace NoMoreBets.Domain.Matches;

public class MatchPreview
{
  public int MatchId { get; set; }
  public string PreviewContentJson { get; set; } = null!;

  public Match Match { get; set; } = null!;
}
