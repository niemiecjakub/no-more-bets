using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Domain.Clubs;

public class Club
{
  public int Id { get; set; }
  public string Name { get; set; } = null!;
  public string Slug { get; set; } = null!;
  public int SoccerdataId { get; set; }

  public ICollection<ClubSeason> ClubSeasons { get; set; } = new List<ClubSeason>();
  public ICollection<Match> HomeMatches { get; set; } = new List<Match>();
  public ICollection<Match> AwayMatches { get; set; } = new List<Match>();
  public ICollection<LeagueTableSnapshotRow> LeagueTableSnapshotRows { get; set; } = new List<LeagueTableSnapshotRow>();
  public ICollection<ClubDailySummary> ClubDailySummaries { get; set; } = new List<ClubDailySummary>();
}
