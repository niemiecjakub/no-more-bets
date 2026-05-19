using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;
public class ClubRepository : IClubRepository
{
  private readonly AppDbContext _db;
  public ClubRepository(AppDbContext db)
  {
    _db = db;
  }

  public async Task AddHead2Head(Head2Head head2Head)
  {
    await _db.Head2Head.AddAsync(head2Head);
  }

  public async Task<Club?> GetByIdAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _db.Club
      .Include(c => c.League)
      .FirstOrDefaultAsync(c => c.Id == clubId, cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ClubLeagueStats?> GetCurrentClubLeagueStatsAsync(int clubId, DateOnly? date = null, CancellationToken cancellationToken = default)
  {
    var query = _db.LeagueTableSnapshotRow
      .Where(r => r.ClubId == clubId);

    if (date.HasValue)
      query = query.Where(r => r.Snapshot.SnapshotDate <= date.Value);

    return await query
      .OrderByDescending(r => r.Snapshot.SnapshotDate)
      .ThenByDescending(r => r.SnapshotId)
      .Select(r => new ClubLeagueStats(r))
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ClubDailySummary?> GetDailySummaryAsync(int clubId, DateOnly? date = null, CancellationToken cancellationToken = default)
  {
    var query = _db.ClubDailySummary
      .Where(s => s.ClubId == clubId);

    if (date.HasValue)
      query = query.Where(s => s.Date <= date.Value);

    return await query
      .OrderByDescending(s => s.Date)
      .FirstOrDefaultAsync(cancellationToken);
  }

  public async Task AddDailySummaryAsync(ClubDailySummary summary, CancellationToken cancellationToken = default)
  {
    await _db.ClubDailySummary.AddAsync(summary, cancellationToken);
  }

  public Task<List<Club>> GetBySoccerdataId(IEnumerable<int> soccerdataIds)
  {
    return _db.Club
      .Where(c => soccerdataIds.Contains(c.SoccerdataId))
      .ToListAsync();
  }

  public async Task<IReadOnlyList<Club>> GetClubsWithLeagueOrderedByNameAsync(CancellationToken cancellationToken = default)
  {
    return await _db.Club
      .AsNoTracking()
      .Include(c => c.League)
      .OrderBy(c => c.Name)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public Task<List<Club>> GetClubs(int? leagueId = null)
  {
    var query = _db.Club.AsQueryable();
    if (leagueId.HasValue)
      query = query.Where(c => c.LeagueId == leagueId.Value);
    return query.ToListAsync();
  }
}
