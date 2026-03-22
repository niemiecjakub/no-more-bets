using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Clubs;
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

  public async Task<IReadOnlyList<LeagueTableStanding>?> GetLeagueTableAsOfAsync(int leagueId, DateOnly asOfDate, CancellationToken cancellationToken = default)
  {
    var snapshot = await _db.LeagueTableSnapshot
      .AsNoTracking()
      .Where(s => s.LeagueId == leagueId && s.SnapshotDate <= asOfDate)
      .OrderByDescending(s => s.SnapshotDate)
      .Include(s => s.Rows)
      .ThenInclude(r => r.Club)
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);

    if (snapshot is null)
      return null;

    return snapshot.Rows
      .OrderBy(r => r.Position)
      .Select(r => new LeagueTableStanding(r.ClubId, r.Club.Name, new ClubLeagueStats(r)))
      .ToList();
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

  public async Task AddLeagueTableSnapshot(LeagueTableSnapshot snapshot)
  {
    await _db.LeagueTableSnapshot.AddAsync(snapshot);
  }
}
