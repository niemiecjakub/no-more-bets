using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Bankrolls;
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
}
