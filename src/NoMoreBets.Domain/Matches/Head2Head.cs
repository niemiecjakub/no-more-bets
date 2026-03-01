using System.Text.Json;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Domain.Matches;

public class Head2Head
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public int Team1Id { get; set; }
  public int Team2Id { get; set; }
  public string Head2HeadJson { get; set; } = null!;
  public DateTime UpdatedAt { get; set; }

  public Club Team1 { get; set; } = null!;
  public Club Team2 { get; set; } = null!;

  /// <summary>
  /// Deserializes <see cref="Head2HeadJson"/> and aligns teams so that team 1 is the club with
  /// <paramref name="team1SoccerdataId"/> and team 2 is the club with <paramref name="team2SoccerdataId"/>,
  /// ensuring entity Team1SoccerdataId/Team2SoccerdataId correspond to the returned Team1/Team2 and their stats.
  /// </summary>
  public HeadToHead GetHeadToHead(int team1SoccerdataId, int team2SoccerdataId)
  {
    var raw = JsonSerializer.Deserialize<HeadToHead>(Head2HeadJson, JsonOptions);
    if (raw == null)
      return new HeadToHead { Team1 = new TeamInfo(), Team2 = new TeamInfo(), Stats = new HeadToHeadStats() };

    return raw.Team1.Id == team1SoccerdataId ? raw : Swap(raw);
  }

  private static HeadToHead Swap(HeadToHead raw)
  {
    var stats = raw.Stats;
    var swappedStats = new HeadToHeadStats
    {
      Overall = new OverallStats
      {
        OverallGamesPlayed = stats.Overall.OverallGamesPlayed,
        OverallTeam1Wins = stats.Overall.OverallTeam2Wins,
        OverallTeam2Wins = stats.Overall.OverallTeam1Wins,
        OverallDraws = stats.Overall.OverallDraws,
        OverallTeam1Scored = stats.Overall.OverallTeam2Scored,
        OverallTeam2Scored = stats.Overall.OverallTeam1Scored
      },
      Team1AtHome = new Team1AtHomeStats
      {
        Team1GamesPlayedAtHome = stats.Team2AtHome.Team2GamesPlayedAtHome,
        Team1WinsAtHome = stats.Team2AtHome.Team2WinsAtHome,
        Team1LossesAtHome = stats.Team2AtHome.Team2LossesAtHome,
        Team1DrawsAtHome = stats.Team2AtHome.Team2DrawsAtHome,
        Team1ScoredAtHome = stats.Team2AtHome.Team2ScoredAtHome,
        Team1ConcededAtHome = stats.Team2AtHome.Team2ConcededAtHome
      },
      Team2AtHome = new Team2AtHomeStats
      {
        Team2GamesPlayedAtHome = stats.Team1AtHome.Team1GamesPlayedAtHome,
        Team2WinsAtHome = stats.Team1AtHome.Team1WinsAtHome,
        Team2LossesAtHome = stats.Team1AtHome.Team1LossesAtHome,
        Team2DrawsAtHome = stats.Team1AtHome.Team1DrawsAtHome,
        Team2ScoredAtHome = stats.Team1AtHome.Team1ScoredAtHome,
        Team2ConcededAtHome = stats.Team1AtHome.Team1ConcededAtHome
      }
    };
    return new HeadToHead { Team1 = raw.Team2, Team2 = raw.Team1, Stats = swappedStats };
  }

  public static (int Team1Id, int Team2Id) NormalizeClubIds(int club1Id, int club2Id)
  {
    return (Math.Min(club1Id, club2Id), Math.Max(club1Id, club2Id));
  }
}

public static class Head2HeadQueryableExtensions
{
  public static IQueryable<Head2Head> ForClubs(this IQueryable<Head2Head> query, int club1Id, int club2Id)
  {
    var (team1Id, team2Id) = Head2Head.NormalizeClubIds(club1Id, club2Id);
    return query.Where(h => h.Team1Id == team1Id && h.Team2Id == team2Id);
  }
}
