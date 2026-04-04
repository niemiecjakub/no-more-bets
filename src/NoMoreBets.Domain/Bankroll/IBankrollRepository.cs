namespace NoMoreBets.Domain.Bankrolls;

public interface IBankrollRepository
{
  Task<decimal> GetCurrentBalanceAsync(CancellationToken cancellationToken = default);
}
