using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Domain.Leagues;
public interface ILeagueRepository
{
  Task<Season?> GetSeasonForDateAsync(int leagueId, DateOnly date);
  Task<IReadOnlyList<int>> GetLatestSeasonIdsAsync(CancellationToken cancellationToken = default);
  Task<bool> TableSnapshotExists(int seasonId, DateOnly date);
  Task<LeagueTableSnapshot?> GetLatestTableSnapshot(int seasonId);
  public Task<IReadOnlyList<LeagueTableStanding>?> GetLeagueTableAsOfAsync(int leagueId, DateOnly? asOfDate, CancellationToken cancellationToken = default);
  Task<Stage> GetStageForDateAsync(int soccerdataLeagueId, DateOnly date);
  public Task<List<League>> GetLeagues();
  Task<IReadOnlyList<League>> GetLeaguesOrderedByNameAsync(CancellationToken cancellationToken = default);
  Task<LeagueTableSnapshot?> GetLatestLeagueTableSnapshotAsync(
    int leagueId,
    int seasonId,
    CancellationToken cancellationToken = default);
  public Task<League?> GetByIdAsync(int leagueId, CancellationToken cancellationToken = default);
  public Task AddLeagueTableSnapshot(LeagueTableSnapshot snapshot);
}
