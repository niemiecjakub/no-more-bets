using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;

public class BankrollRepository : IBankrollRepository
{
  private readonly AppDbContext _db;

  public BankrollRepository(AppDbContext db)
  {
    _db = db;
  }

  public async Task<decimal> GetCurrentBalanceAsync(CancellationToken cancellationToken = default)
  {
    var rows = await _db.Bankroll
      .AsNoTracking()
      .GroupBy(e => e.Flow)
      .Select(g => new { Flow = g.Key, Sum = g.Sum(e => e.Amount) })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var totalIn = rows.Where(r => r.Flow == BankrollFlowExtensions.InCode).Select(r => r.Sum).FirstOrDefault();
    var totalOut = rows.Where(r => r.Flow == BankrollFlowExtensions.OutCode).Select(r => r.Sum).FirstOrDefault();
    return totalIn - totalOut;
  }

  public async Task<IReadOnlyList<Bankroll>> GetAllOrderedByCreatedAtDescAsync(
    CancellationToken cancellationToken = default)
  {
    return await _db.Bankroll
      .AsNoTracking()
      .OrderByDescending(b => b.CreatedAt)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task AddAsync(Bankroll entity, CancellationToken cancellationToken = default)
  {
    await _db.Bankroll.AddAsync(entity, cancellationToken).ConfigureAwait(false);
  }

  public async Task<decimal> GetTotalValueAsync(CancellationToken cancellationToken = default)
  {
    return (await _db.Bankroll
      .AsNoTracking()
      .SumAsync(
        record => (decimal?)(record.Flow == BankrollFlowExtensions.InCode ? record.Amount : -record.Amount),
        cancellationToken)
      .ConfigureAwait(false)) ?? 0m;
  }

  public async Task<decimal> GetBettingBalanceAsync(CancellationToken cancellationToken = default)
  {
    return (await _db.Bankroll
      .AsNoTracking()
      .Where(record => record.BetId != null)
      .SumAsync(
        record => (decimal?)(record.Flow == BankrollFlowExtensions.InCode ? record.Amount : -record.Amount),
        cancellationToken)
      .ConfigureAwait(false)) ?? 0m;
  }

  public async Task<BankrollPage> GetEntriesPageAsync(
    int limit,
    DateTime? afterCreatedAtUtc,
    int? afterId,
    IReadOnlyCollection<string>? entryNames = null,
    CancellationToken cancellationToken = default)
  {
    var query = _db.Bankroll.AsNoTracking();
    if (entryNames is { Count: > 0 })
      query = query.Where(row => entryNames.Contains(row.Name));

    if (afterCreatedAtUtc is not null && afterId is not null)
    {
      var cursorCreatedAt = afterCreatedAtUtc.Value;
      var cursorId = afterId.Value;
      query = query.Where(row =>
        row.CreatedAt < cursorCreatedAt
        || (row.CreatedAt == cursorCreatedAt && row.Id < cursorId));
    }

    var rows = await query
      .OrderByDescending(row => row.CreatedAt)
      .ThenByDescending(row => row.Id)
      .Take(limit + 1)
      .Select(row => new BankrollEntryRow(
        row.Id,
        row.Name,
        row.Amount,
        row.Flow,
        row.CreatedAt,
        row.BetId))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var hasMore = rows.Count > limit;
    if (hasMore)
      rows.RemoveAt(rows.Count - 1);

    return new BankrollPage(rows, hasMore);
  }

  public async Task<BetSlip?> GetBettingPhaseBetSlipForEntryAsync(
    int entryId,
    CancellationToken cancellationToken = default)
  {
    var entry = await _db.Bankroll
      .AsNoTracking()
      .Where(row => row.Id == entryId)
      .Select(row => new { row.Id, row.BetId })
      .SingleOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);

    if (entry is null || entry.BetId is null)
      return null;

    return await _db.BetSlip
      .AsNoTracking()
      .Where(slip => slip.Id == entry.BetId.Value)
      .Where(slip => slip.AgentSession != null && slip.AgentSession.Phase == AgentSessionPhase.Betting)
      .Include(slip => slip.BetStatusEntity)
      .Include(slip => slip.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.HomeClub)
      .Include(slip => slip.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.AwayClub)
      .Include(slip => slip.Selections)
        .ThenInclude(sel => sel.BetStatusEntity)
      .SingleOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);
  }
}
