using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardPendingBets;

public record GetAgentDashboardPendingBetsQuery : IRequest<AgentDashboardPendingBetsDto>;

public sealed class GetAgentDashboardPendingBetsHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetAgentDashboardPendingBetsQuery, AgentDashboardPendingBetsDto>
{
  public async Task<AgentDashboardPendingBetsDto> Handle(
    GetAgentDashboardPendingBetsQuery request,
    CancellationToken cancellationToken)
  {
    var data = await unitOfWork.Betting
      .GetBettingPhasePendingBetsWidgetAsync(cancellationToken)
      .ConfigureAwait(false);

    return new AgentDashboardPendingBetsDto(
      data.PendingSlipsCount,
      data.PendingStakeTotal,
      data.PendingPotentialPayoutTotal,
      data.LatestPendingCreatedAt);
  }
}
