using NoMoreBets.Domain.Clubs;

namespace NoMoreBets.Domain.Matches;

public class Head2Head
{
  public int Team1Id { get; set; }
  public int Team2Id { get; set; }
  public string Head2HeadJson { get; set; } = null!;
  public DateTime UpdatedAt { get; set; }

  public Club Team1 { get; set; } = null!;
  public Club Team2 { get; set; } = null!;

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
