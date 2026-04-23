using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.AgentSessions;
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

  public async Task<BetSlip?> GetBetSlipWithSelectionsByIdAsync(int betSlipId, CancellationToken cancellationToken = default)
  {
    return await _db.BetSlip
      .Include(s => s.Selections)
      .FirstOrDefaultAsync(s => s.Id == betSlipId, cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<BetSlip>> GetBetSlipsAsync(BetStatus? slipStatus = null, CancellationToken cancellationToken = default)
  {
    var query = _db.BetSlip.Where(s => s.AgentSession == null || s.AgentSession.Phase != AgentSessionPhase.Research);
    if (slipStatus is { } status)
    {
      query = query.Where(s => s.StatusId == (int)status);
    }

    return await MaterializeBetSlipListAsync(query, cancellationToken).ConfigureAwait(false);
  }

  public async Task<BetSlip?> GetLatestResearchBetSlipForMatchAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _db.BetSlip
      .AsSplitQuery()
      .Where(s => s.AgentSession != null && s.AgentSession.Phase == AgentSessionPhase.Research)
      .Where(s => s.Selections.Any(sel => sel.MatchId == matchId))
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.AwayClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.EventTypeEntity)
      .OrderByDescending(s => s.CreatedAt)
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  private async Task<IReadOnlyList<BetSlip>> MaterializeBetSlipListAsync(
    IQueryable<BetSlip> query,
    CancellationToken cancellationToken)
  {
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

  public async Task<IReadOnlyList<BetSlip>> GetNonPendingBetSlipsAwaitingReflectionAsync(
    CancellationToken cancellationToken = default)
  {
    return await _db.BetSlip
      .Where(s =>
        s.StatusId != (int)BetStatus.Pending
        && s.AgentSessionReflectedId == null
        && (s.AgentSession == null || s.AgentSession.Phase != AgentSessionPhase.Research))
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.AwayClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.EventTypeEntity)
      .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task MarkBetSlipsAgentSessionReflectedAsync(
    int agentSessionId,
    IReadOnlyList<int> betSlipIds,
    CancellationToken cancellationToken = default)
  {
    if (betSlipIds.Count == 0)
    {
      return;
    }

    var distinctIds = betSlipIds.Distinct().ToList();
    var utcNow = DateTime.UtcNow;
    await _db.BetSlip
      .Where(s => distinctIds.Contains(s.Id))
      .ExecuteUpdateAsync(
        s => s
          .SetProperty(b => b.AgentSessionReflectedId, agentSessionId)
          .SetProperty(b => b.UpdatedAt, utcNow),
        cancellationToken)
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
        .ThenInclude(b => b.AgentSession)
      .Include(s => s.BetSlip)
      .ThenInclude(b => b.Selections)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }
}
