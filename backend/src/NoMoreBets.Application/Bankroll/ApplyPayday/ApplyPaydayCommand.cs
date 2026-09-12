using MediatR;
using NoMoreBets.Domain.Bankrolls;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Bankroll.ApplyPayday;

public record ApplyPaydayCommand : IRequest<Unit>;

public sealed class ApplyPaydayHandler(
  IBankrollRepository bankroll, IUnitOfWork unitOfWork,
  ILogger<ApplyPaydayHandler> logger) : IRequestHandler<ApplyPaydayCommand, Unit>
{
  public async Task<Unit> Handle(ApplyPaydayCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Handling {HandlerName}", nameof(ApplyPaydayHandler));

    var entry = NoMoreBets.Domain.Bankrolls.Bankroll.CreateSalary();
    await bankroll.AddAsync(entry, cancellationToken).ConfigureAwait(false);
    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation(
      "Recorded salary bankroll entry: Name={Name}, Amount={Amount}",
      entry.Name,
      entry.Amount);

    return Unit.Value;
  }
}
