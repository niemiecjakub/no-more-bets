using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Domain.Clubs;
public interface IClubRepository
{
  public Task<List<Club>> GetClubs(int leagueId);
  public Task<List<Club>> GetBySoccerdataId(IEnumerable<int> soccerdataIds);
  public Task AddHead2Head(Head2Head head2Head);
}
