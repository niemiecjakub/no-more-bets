namespace NoMoreBets.Domain.Entity;

public class Game
{
  public int Id { get; set; }
  public int? SoccerdataId { get; set; }
  public int StageId { get; set; }
  public DateTime MatchDate { get; set; }
  public int HomeClubId { get; set; }
  public int AwayClubId { get; set; }
  public string Status { get; set; } = null!;
  public int HomeGoals { get; set; }
  public int AwayGoals { get; set; }

  public Stage Stage { get; set; } = null!;
  public Club HomeClub { get; set; } = null!;
  public Club AwayClub { get; set; } = null!;
}
