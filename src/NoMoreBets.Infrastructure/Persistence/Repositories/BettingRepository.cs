using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

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
      .Include(s => s.Rows)
      .ThenInclude(r => r.EventOptionEntity)
      .OrderByDescending(s => s.SnapshotTime)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<decimal?> GetCurrentOddsForSelectionAsync(int matchId, BettingEventType eventType, BettingEventOption eventOption, CancellationToken cancellationToken = default)
  {
    var snapshots = await GetBettingOddsSnapshotsForMatchAsync(matchId, cancellationToken).ConfigureAwait(false);
    if (snapshots.Count == 0)
      return null;

    var latest = snapshots[0];
    var eventTypeId = (int)eventType;
    var optionId = (int)eventOption;
    foreach (var row in latest.Rows.Where(r => r.EventTypeId == eventTypeId && r.EventOptionId == optionId))
    {
      if (row.Odds.HasValue)
        return row.Odds.Value;
    }

    return null;
  }

  public async Task<IReadOnlyList<Match>> GetMatchesAvailableForBettingAsync(CancellationToken cancellationToken = default)
  {
    var matchIdsWithSnapshots = _db.BettingOddsSnapshot.Select(s => s.MatchId).Distinct();
    var matchIdsWithAnalysis = _db.MatchAnalysis.Select(a => a.MatchId).Distinct();

    return await _db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming
        && matchIdsWithSnapshots.Contains(m.Id)
        && matchIdsWithAnalysis.Contains(m.Id))
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task AddBetSlipAsync(BetSlip slip, CancellationToken cancellationToken = default)
  {
    await _db.BetSlip.AddAsync(slip, cancellationToken).ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<BetSlip>> GetBetSlipsAsync(BetStatus? slipStatus = null, CancellationToken cancellationToken = default)
  {
    var query = _db.BetSlip.AsQueryable();
    if (slipStatus is { } status)
      query = query.Where(s => s.StatusId == (int)status);

    return await query
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.AwayClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.EventTypeEntity)
      .OrderByDescending(s => s.CreatedAt)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<BetSlip>> GetNonPendingBetSlipsCreatedInLastDaysAsync(int lastDays, CancellationToken cancellationToken = default)
  {
    var sinceUtc = DateTime.UtcNow.AddDays(-lastDays);
    return await _db.BetSlip
      .Where(s => s.StatusId != (int)BetStatus.Pending && s.CreatedAt >= sinceUtc)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.AwayClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.EventTypeEntity)
      .OrderByDescending(s => s.CreatedAt)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<BetSlip>> GetNonPendingBetSlipsUpdatedInLastDaysAsync(int lastDays, CancellationToken cancellationToken = default)
  {
    var sinceUtc = DateTime.UtcNow.AddDays(-lastDays);
    return await _db.BetSlip
      .Where(s => s.StatusId != (int)BetStatus.Pending && s.UpdatedAt != null && s.UpdatedAt >= sinceUtc)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.AwayClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.EventTypeEntity)
      .OrderByDescending(s => s.UpdatedAt)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<BetSelection>> GetPendingSelectionsWithBothScoresAsync(
    CancellationToken cancellationToken = default)
  {
    return await _db.BetSelection
      .Where(s => s.StatusId == (int)BetStatus.Pending)
      .Where(s => s.Match.HomeGoals != null && s.Match.AwayGoals != null)
      .Include(s => s.Match)
      .Include(s => s.BetSlip)
      .ThenInclude(b => b.Selections)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }
}
