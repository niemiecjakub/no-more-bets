namespace NoMoreBets.Domain.Entity;

public class Lineup
{
  public int MatchId { get; set; }
  public string HomeTeamJson { get; set; } = null!;
  public string AwayTeamJson { get; set; } = null!;

  public Match Match { get; set; } = null!;
}
