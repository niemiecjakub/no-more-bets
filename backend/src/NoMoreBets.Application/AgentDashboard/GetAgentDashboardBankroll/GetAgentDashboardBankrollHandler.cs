using MediatR;
using NoMoreBets.Application.Bankroll.GetDaysUntilPayday;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardBankroll;

public record GetAgentDashboardBankrollQuery : IRequest<AgentDashboardBankrollDto>;

public sealed class GetAgentDashboardBankrollHandler(IUnitOfWork unitOfWork, IMediator mediator)
  : IRequestHandler<GetAgentDashboardBankrollQuery, AgentDashboardBankrollDto>
{
  public async Task<AgentDashboardBankrollDto> Handle(
    GetAgentDashboardBankrollQuery request,
    CancellationToken cancellationToken)
  {
    var totalValue = await unitOfWork.Bankroll
      .GetTotalValueAsync(cancellationToken)
      .ConfigureAwait(false);
    var balance = await unitOfWork.Bankroll
      .GetBettingBalanceAsync(cancellationToken)
      .ConfigureAwait(false);
    var daysUntilPayday = await mediator
      .Send(new GetDaysUntilPaydayQuery(), cancellationToken)
      .ConfigureAwait(false);

    return new AgentDashboardBankrollDto(totalValue, balance, daysUntilPayday);
  }
}
