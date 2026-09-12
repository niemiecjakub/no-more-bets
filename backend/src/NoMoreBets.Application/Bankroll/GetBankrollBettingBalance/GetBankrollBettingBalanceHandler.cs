using MediatR;
using NoMoreBets.Domain.Bankrolls;

namespace NoMoreBets.Application.Bankroll.GetBankrollBettingBalance;

public record GetBankrollBettingBalanceQuery : IRequest<BankrollBettingBalanceDto>;

public sealed class GetBankrollBettingBalanceHandler(IBankrollRepository bankroll)
  : IRequestHandler<GetBankrollBettingBalanceQuery, BankrollBettingBalanceDto>
{
  public async Task<BankrollBettingBalanceDto> Handle(
    GetBankrollBettingBalanceQuery request,
    CancellationToken cancellationToken)
  {
    var balance = await bankroll
      .GetBettingBalanceAsync(null, cancellationToken)
      .ConfigureAwait(false);

    return new BankrollBettingBalanceDto(balance);
  }
}
