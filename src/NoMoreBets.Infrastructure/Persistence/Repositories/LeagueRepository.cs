using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;
public class LeagueRepository : ILeagueRepository
{
  private readonly AppDbContext _db;
  public LeagueRepository(AppDbContext db)
  {
    _db = db;
  }

  public Task<Season?> GetLatestSeason(int leagueId)
  {
    return _db.Season
      .Where(s => s.LeagueId == leagueId)
      .OrderByDescending(s => s.Id)
      .FirstOrDefaultAsync();
  }
  public Task<bool> TableSnapshotExists(int leagueId, DateOnly date)
  {
    return _db.LeagueTableSnapshot.AnyAsync(s => s.LeagueId == leagueId && s.SnapshotDate == date);
  }

  public Task<LeagueTableSnapshot?> GetLatestTableSnapshot(int leagueId)
  {
    return _db.LeagueTableSnapshot
      .Where(s => s.LeagueId == leagueId)
      .Include(s => s.Rows)
      .OrderByDescending(s => s.SnapshotDate)
      .FirstOrDefaultAsync();
  }

  public Task<List<League>> GetLeagues()
  {
    return _db.League.ToListAsync();
  }

  public Task<Stage> GetCurrentStage(int leagueId)
  {
    return _db.Stage
         .Where(s => s.Season.League.SoccerdataId == leagueId)
         .OrderByDescending(s => s.Id)
         .FirstAsync();
  }
}
