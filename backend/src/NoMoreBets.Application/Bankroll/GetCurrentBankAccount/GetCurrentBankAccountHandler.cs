using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Bankroll.GetCurrentBankAccount;

public record GetCurrentBankAccountQuery : IRequest<decimal>;

public sealed class GetCurrentBankAccountHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetCurrentBankAccountQuery, decimal>
{
  public async Task<decimal> Handle(
    GetCurrentBankAccountQuery request,
    CancellationToken cancellationToken)
  {
    return await unitOfWork.Bankroll
      .GetCurrentBalanceAsync(cancellationToken)
      .ConfigureAwait(false);
  }
}
