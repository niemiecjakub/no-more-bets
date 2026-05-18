using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Bankroll.GetBankrollBettingBalance;

public record GetBankrollBettingBalanceQuery : IRequest<BankrollBettingBalanceDto>;

public sealed class GetBankrollBettingBalanceHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetBankrollBettingBalanceQuery, BankrollBettingBalanceDto>
{
  public async Task<BankrollBettingBalanceDto> Handle(
    GetBankrollBettingBalanceQuery request,
    CancellationToken cancellationToken)
  {
    var balance = await unitOfWork.Bankroll
      .GetBettingBalanceAsync(cancellationToken)
      .ConfigureAwait(false);

    return new BankrollBettingBalanceDto(balance);
  }
}
