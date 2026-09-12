namespace NoMoreBets.Application.Common;

public interface IUnitOfWork
{
  Task SaveChangesAsync(CancellationToken cancellationToken);
}
