using NoMoreBets.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoMoreBets.Domain.Entity;

public class Match
{
  public int Id { get; set; }
  public int SoccerdataId { get; set; }
  public int StageId { get; set; }
  public DateTime MatchDate { get; set; }
  public int HomeClubId { get; set; }
  public int AwayClubId { get; set; }
  public int MatchStatusId { get; set; }
  public int? HomeGoals { get; set; }
  public int? AwayGoals { get; set; }

  public Stage Stage { get; set; } = null!;
  public Club HomeClub { get; set; } = null!;
  public Club AwayClub { get; set; } = null!;
  public MatchStatusEntity MatchStatus { get; set; } = null!;

  [NotMapped]
  public MatchStatus StatusEnum
  {
    get => (MatchStatus)MatchStatusId;
    set => MatchStatusId = (int)value;
  }
}
