using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Leagues;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoMoreBets.Domain.Matches;

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
  public string? BetclicUrl { get; set; }

  public Stage? Stage { get; set; } = null!;
  public Club HomeClub { get; set; } = null!;
  public Club AwayClub { get; set; } = null!;
  public MatchStatusEntity MatchStatusEntity { get; set; } = null!;
  public Lineup? Lineup { get; set; }
  public MatchPreview? MatchPreview { get; set; }
  public MatchDetails? MatchDetails { get; set; }
  public ICollection<MatchAnalysis> MatchAnalyses { get; set; } = new List<MatchAnalysis>();
  public ICollection<BettingOddsSnapshot> BettingOddsSnapshots { get; set; } = new List<BettingOddsSnapshot>();
  public ICollection<BetSelection> BetSelections { get; set; } = new List<BetSelection>();
  public ICollection<MatchEvent> MatchEvents { get; set; } = new List<MatchEvent>();

  [NotMapped]
  public MatchStatus MatchStatus
  {
    get => (MatchStatus)MatchStatusId;
    set => MatchStatusId = (int)value;
  }

  [NotMapped]
  public bool IsFifaWorldCup => Stage?.Season?.League?.Slug == League.FifaWorldCupSlug;

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

public static class MatchQueryableExtensions
{
  /// <summary>
  /// Filters Match to rows where the two clubs are home and away (order-independent).
  /// </summary>
  public static IQueryable<Match> ForClubs(this IQueryable<Match> query, int club1Id, int club2Id) =>
    query.Where(m =>
      (m.HomeClubId == club1Id && m.AwayClubId == club2Id) ||
      (m.HomeClubId == club2Id && m.AwayClubId == club1Id));
}
