namespace NoMoreBets.Domain.Matches;
public interface IMatchRepository
{
  public Task<Match?> GetMatchBySoccerdataId(int soccerdataId);
  public Task<MatchPreview?> GetMatchPreview(int matchId);
  public Task<Head2Head?> GetHeadToHead(int team1, int team2);
  public Task<List<Match>> GetMatches(DateTime date);
  public Task<Lineup?> GetLineup(int matchId);
}
