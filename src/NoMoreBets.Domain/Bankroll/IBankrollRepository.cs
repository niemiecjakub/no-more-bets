namespace NoMoreBets.Domain.Bankrolls;

public interface IBankrollRepository
{
  Task<decimal> GetCurrentBalanceAsync(CancellationToken cancellationToken = default);
  Task AddAsync(Bankroll entity, CancellationToken cancellationToken = default);
}
