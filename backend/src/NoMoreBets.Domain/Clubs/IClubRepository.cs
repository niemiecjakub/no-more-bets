using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Domain.Clubs;
public interface IClubRepository
{
  Task<IReadOnlyList<Club>> GetClubsWithMembershipsOrderedByNameAsync(CancellationToken cancellationToken = default);
  Task<List<Club>> GetClubs();
  Task<List<Club>> GetClubsForSeasonAsync(int seasonId);
  public Task<List<Club>> GetBySoccerdataId(IEnumerable<int> soccerdataIds);
  public Task<Club?> GetByIdAsync(int clubId, CancellationToken cancellationToken = default);
  public Task<ClubLeagueStats?> GetCurrentClubLeagueStatsAsync(int clubId, DateOnly? date = null, int? seasonId = null, CancellationToken cancellationToken = default);
  public Task<ClubDailySummary?> GetDailySummaryAsync(int clubId, DateOnly? date = null, CancellationToken cancellationToken = default);
  public Task AddDailySummaryAsync(ClubDailySummary summary, CancellationToken cancellationToken = default);
  public Task AddHead2Head(Head2Head head2Head);
  Task AddClubAsync(Club club, CancellationToken cancellationToken = default);
}
