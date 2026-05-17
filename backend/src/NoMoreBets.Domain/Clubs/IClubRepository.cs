using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Domain.Clubs;
public interface IClubRepository
{
  public Task<List<Club>> GetClubs(int? leagueId = null);
  public Task<List<Club>> GetBySoccerdataId(IEnumerable<int> soccerdataIds);
  public Task<Club?> GetByIdAsync(int clubId, CancellationToken cancellationToken = default);
  public Task<ClubLeagueStats?> GetCurrentClubLeagueStatsAsync(int clubId, DateOnly? date = null, CancellationToken cancellationToken = default);
  public Task<ClubDailySummary?> GetDailySummaryAsync(int clubId, DateOnly? date = null, CancellationToken cancellationToken = default);
  public Task AddDailySummaryAsync(ClubDailySummary summary, CancellationToken cancellationToken = default);
  public Task AddHead2Head(Head2Head head2Head);
}
