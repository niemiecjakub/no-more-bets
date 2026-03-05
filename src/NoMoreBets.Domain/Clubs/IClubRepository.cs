using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Domain.Clubs;
public interface IClubRepository
{
  public Task<List<Club>> GetClubs(int leagueId);
  public Task<List<Club>> GetBySoccerdataId(IEnumerable<int> soccerdataIds);
  public Task<Club?> GetByIdAsync(int clubId, CancellationToken cancellationToken = default);
  public Task<ClubDailySummary?> GetLatestDailySummaryAsync(int clubId, CancellationToken cancellationToken = default);
  public Task AddDailySummaryAsync(ClubDailySummary summary, CancellationToken cancellationToken = default);
  public Task AddHead2Head(Head2Head head2Head);
}
