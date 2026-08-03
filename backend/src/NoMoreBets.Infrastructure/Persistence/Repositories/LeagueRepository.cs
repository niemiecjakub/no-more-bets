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

  public Task<Season?> GetSeasonForDateAsync(int leagueId, DateOnly date)
  {
    return _db.Season
      .Where(s => s.LeagueId == leagueId
        && (s.StartDate == null || s.StartDate <= date)
        && (s.EndDate == null || s.EndDate >= date))
      .OrderByDescending(s => s.StartDate)
      .ThenByDescending(s => s.Id)
      .FirstOrDefaultAsync();
  }

  public Task<Season?> GetLatestSeasonAsync(int leagueId, CancellationToken cancellationToken = default)
  {
    return _db.Season
      .Where(s => s.LeagueId == leagueId)
      .OrderByDescending(s => s.StartDate)
      .ThenByDescending(s => s.Id)
      .FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<int>> GetLatestSeasonIdsAsync(CancellationToken cancellationToken = default)
  {
    var seasons = await _db.Season
      .AsNoTracking()
      .Select(s => new { s.Id, s.LeagueId, s.StartDate })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return seasons
      .GroupBy(s => s.LeagueId)
      .Select(g => g
        .OrderByDescending(s => s.StartDate)
        .ThenByDescending(s => s.Id)
        .First().Id)
      .ToList();
  }

  public async Task<IReadOnlyList<string>> GetSeasonYearsOrderedLatestFirstAsync(
    CancellationToken cancellationToken = default)
  {
    var seasons = await _db.Season
      .AsNoTracking()
      .Select(s => new { s.Year, s.StartDate })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return seasons
      .GroupBy(s => s.Year)
      .Select(g => (Year: g.Key, MaxStartDate: g.Max(s => s.StartDate)))
      .OrderByDescending(x => x.MaxStartDate)
      .ThenByDescending(x => x.Year)
      .Select(x => x.Year)
      .ToList();
  }

  public Task<bool> TableSnapshotExists(int seasonId, DateOnly date)
  {
    return _db.LeagueTableSnapshot.AnyAsync(s => s.SeasonId == seasonId && s.SnapshotDate == date);
  }

  public Task<LeagueTableSnapshot?> GetLatestTableSnapshot(int seasonId)
  {
    return _db.LeagueTableSnapshot
      .Where(s => s.SeasonId == seasonId)
      .Include(s => s.Rows)
      .OrderByDescending(s => s.SnapshotDate)
      .FirstOrDefaultAsync();
  }

  public async Task<IReadOnlyList<LeagueTableStanding>?> GetLeagueTableAsOfAsync(int leagueId, DateOnly? asOfDate, CancellationToken cancellationToken = default)
  {
    var snapshot = await _db.LeagueTableSnapshot
      .AsNoTracking()
      .Where(s => s.LeagueId == leagueId && (asOfDate == null || s.SnapshotDate <= asOfDate))
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

  public async Task<IReadOnlyList<League>> GetLeaguesOrderedByNameAsync(CancellationToken cancellationToken = default)
  {
    return await _db.League
      .AsNoTracking()
      .OrderBy(l => l.Name)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public Task<LeagueTableSnapshot?> GetLatestLeagueTableSnapshotAsync(
    int leagueId,
    int seasonId,
    CancellationToken cancellationToken = default)
  {
    return _db.LeagueTableSnapshot
      .AsNoTracking()
      .Where(s => s.LeagueId == leagueId && s.SeasonId == seasonId)
      .Include(s => s.League)
      .Include(s => s.Rows)
      .ThenInclude(r => r.Club)
      .OrderByDescending(s => s.SnapshotDate)
      .FirstOrDefaultAsync(cancellationToken);
  }

  public Task<League?> GetByIdAsync(int leagueId, CancellationToken cancellationToken = default)
  {
    return _db.League.AsNoTracking().FirstOrDefaultAsync(l => l.Id == leagueId, cancellationToken);
  }

  public Task<Stage> GetStageForDateAsync(int soccerdataLeagueId, DateOnly date)
  {
    return _db.Stage
      .Where(s => s.Season.League.SoccerdataId == soccerdataLeagueId
        && (s.Season.StartDate == null || s.Season.StartDate <= date)
        && (s.Season.EndDate == null || s.Season.EndDate >= date))
      .OrderByDescending(s => s.Season.StartDate)
      .ThenByDescending(s => s.Id)
      .FirstAsync();
  }

  public async Task AddLeagueTableSnapshot(LeagueTableSnapshot snapshot)
  {
    await _db.LeagueTableSnapshot.AddAsync(snapshot);
  }
}
