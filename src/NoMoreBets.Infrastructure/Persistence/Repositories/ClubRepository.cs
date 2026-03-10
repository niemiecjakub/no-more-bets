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
      .FirstOrDefaultAsync(c => c.Id == clubId, cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ClubLeagueStats?> GetCurrentClubLeagueStatsAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _db.LeagueTableSnapshotRow
      .Where(r => r.ClubId == clubId)
      .OrderByDescending(r => r.SnapshotId)
      .Select(r => new ClubLeagueStats(r))
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<ClubDailySummary?> GetLatestDailySummaryAsync(int clubId, CancellationToken cancellationToken = default)
  {
    return await _db.ClubDailySummary
      .Where(s => s.ClubId == clubId)
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

  public Task<List<Club>> GetClubs(int leagueId)
  {
    return _db.Club
      .Where(c => c.LeagueId == leagueId)
      .ToListAsync();
  }
}
