using MediatR;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Application.Bankroll.GetDaysUntilPayday;

namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardBankroll;

public record GetAgentDashboardBankrollQuery(IReadOnlyList<string> SeasonYears)
  : IRequest<AgentDashboardBankrollDto>;

public sealed class GetAgentDashboardBankrollHandler(IBankrollRepository bankroll, IMediator mediator)
  : IRequestHandler<GetAgentDashboardBankrollQuery, AgentDashboardBankrollDto>
{
  public async Task<AgentDashboardBankrollDto> Handle(
    GetAgentDashboardBankrollQuery request,
    CancellationToken cancellationToken)
  {
    var totalValue = await bankroll
      .GetTotalValueAsync(cancellationToken)
      .ConfigureAwait(false);
    var balance = await bankroll
      .GetBettingBalanceAsync(request.SeasonYears, cancellationToken)
      .ConfigureAwait(false);
    var daysUntilPayday = await mediator
      .Send(new GetDaysUntilPaydayQuery(), cancellationToken)
      .ConfigureAwait(false);

    return new AgentDashboardBankrollDto(totalValue, balance, daysUntilPayday);
  }
}
