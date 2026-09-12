using MediatR;
using NoMoreBets.Domain.Betting;

namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardPendingBets;

public record GetAgentDashboardPendingBetsQuery(IReadOnlyList<string> SeasonYears)
  : IRequest<AgentDashboardPendingBetsDto>;

public sealed class GetAgentDashboardPendingBetsHandler(IBettingRepository betting)
  : IRequestHandler<GetAgentDashboardPendingBetsQuery, AgentDashboardPendingBetsDto>
{
  public async Task<AgentDashboardPendingBetsDto> Handle(
    GetAgentDashboardPendingBetsQuery request,
    CancellationToken cancellationToken)
  {
    var data = await betting
      .GetBettingPhasePendingBetsWidgetAsync(request.SeasonYears, cancellationToken)
      .ConfigureAwait(false);

    return new AgentDashboardPendingBetsDto(
      data.PendingSlipsCount,
      data.PendingStakeTotal,
      data.PendingPotentialPayoutTotal,
      data.LatestPendingCreatedAt);
  }
}
