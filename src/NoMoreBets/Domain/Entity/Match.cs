using NoMoreBets.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoMoreBets.Domain.Entity;

public class Match
{
  public int Id { get; set; }
  public int? SoccerdataId { get; set; }
  public int? StageId { get; set; }
  public DateTime MatchDate { get; set; }
  public int HomeClubId { get; set; }
  public int AwayClubId { get; set; }
  public int MatchStatusId { get; set; }
  public int? HomeGoals { get; set; }
  public int? AwayGoals { get; set; }

  public Stage? Stage { get; set; } = null!;
  public Club HomeClub { get; set; } = null!;
  public Club AwayClub { get; set; } = null!;
  public MatchStatusEntity MatchStatusEntity { get; set; } = null!;
  public Lineup? Lineup { get; set; }
  public MatchPreview? MatchPreview { get; set; }

  [NotMapped]
  public MatchStatus MatchStatus
  {
    get => (MatchStatus)MatchStatusId;
    set => MatchStatusId = (int)value;
  }

  public static Match CreateUpcomming(DateTime matchDate, int stageId, int homeClubId, int awayClubId)
  {
    return new Match
    {
      MatchDate = matchDate,
      StageId = stageId,
      HomeClubId = homeClubId,
      AwayClubId = awayClubId,
      MatchStatus = MatchStatus.Upcomming
    };
  }
}
