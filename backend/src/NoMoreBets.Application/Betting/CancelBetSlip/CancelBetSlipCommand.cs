using MediatR;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Betting.CancelBetSlip;

public record CancelBetSlipCommand(int BetSlipId) : IRequest<Unit>;

public sealed class CancelBetSlipHandler(IBettingRepository betting, IBankrollRepository bankroll, IUnitOfWork unitOfWork) : IRequestHandler<CancelBetSlipCommand, Unit>
{
  public async Task<Unit> Handle(CancelBetSlipCommand request, CancellationToken cancellationToken)
  {
    var slip = await betting
      .GetBetSlipWithSelectionsByIdAsync(request.BetSlipId, cancellationToken)
      .ConfigureAwait(false);

    if (slip is null)
    {
      throw new KeyNotFoundException($"Bet slip with id {request.BetSlipId} was not found.");
    }

    if (slip.Selections.Count == 0)
    {
      throw new InvalidOperationException($"Bet slip {request.BetSlipId} has no selections and cannot be canceled.");
    }

    if (slip.Selections.Any(s => s.BetStatus != BetStatus.Pending))
    {
      throw new InvalidOperationException($"Bet slip {request.BetSlipId} cannot be canceled because at least one selection is not pending.");
    }

    var refund = slip.Cancel();
    await bankroll.AddAsync(refund, cancellationToken).ConfigureAwait(false);

    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return Unit.Value;
  }
}
