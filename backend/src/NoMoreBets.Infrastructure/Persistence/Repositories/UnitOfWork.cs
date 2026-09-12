using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
  public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
