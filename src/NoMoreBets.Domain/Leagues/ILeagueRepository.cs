using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Domain.Leagues;
public interface ILeagueRepository
{
  public Task<Season?> GetLatestSeason(int leagueId);
  public Task<bool> TableSnapshotExists(int leagueId, DateOnly date);
  public Task<LeagueTableSnapshot?> GetLatestTableSnapshot(int leagueId);
  public Task<Stage> GetCurrentStage(int leagueId);
  public Task<List<League>> GetLeagues();
  public Task AddLeagueTableSnapshot(LeagueTableSnapshot snapshot);
}
