using NoMoreBets.Domain.Betting;

namespace NoMoreBets.Domain.Bankrolls;

public interface IBankrollRepository
{
  Task<decimal> GetCurrentBalanceAsync(CancellationToken cancellationToken = default);

  Task<IReadOnlyList<Bankroll>> GetAllOrderedByCreatedAtDescAsync(
    CancellationToken cancellationToken = default);

  Task AddAsync(Bankroll entity, CancellationToken cancellationToken = default);
  Task<decimal> GetBettingBalanceAsync(CancellationToken cancellationToken = default);
  Task<decimal> GetTotalValueAsync(CancellationToken cancellationToken = default);
  Task<BankrollPage> GetEntriesPageAsync(
    int limit,
    DateTime? afterCreatedAtUtc,
    int? afterId,
    IReadOnlyCollection<string>? entryNames = null,
    CancellationToken cancellationToken = default);
  Task<BetSlip?> GetBettingPhaseBetSlipForEntryAsync(int entryId, CancellationToken cancellationToken = default);
}
