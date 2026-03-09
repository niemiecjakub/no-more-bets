using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Betting;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;

public class BettingRepository : IBettingRepository
{
  private readonly AppDbContext _db;

  public BettingRepository(AppDbContext db)
  {
    _db = db;
  }

  public async Task<IReadOnlyList<BettingOddsSnapshot>> GetBettingOddsSnapshotsForMatchAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _db.BettingOddsSnapshot
      .Where(s => s.MatchId == matchId)
      .Include(s => s.Rows)
      .ThenInclude(r => r.EventTypeEntity)
      .OrderByDescending(s => s.SnapshotTime)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }
}
