namespace NoMoreBets.Application.Common;

public interface IUnitOfWork
{
  // ponytail: Application stays EF-free. Delete this and call AppDbContext.SaveChangesAsync if Application ever references EF.
  Task SaveChangesAsync(CancellationToken cancellationToken);
}
