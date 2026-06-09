using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardResearchBettingSummary;

public record GetAgentDashboardResearchBettingSummaryQuery(IReadOnlyList<int> LeagueIds)
  : IRequest<AgentDashboardResearchBettingSummaryDto>;

public sealed class GetAgentDashboardResearchBettingSummaryHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetAgentDashboardResearchBettingSummaryQuery, AgentDashboardResearchBettingSummaryDto>
{
  public async Task<AgentDashboardResearchBettingSummaryDto> Handle(
    GetAgentDashboardResearchBettingSummaryQuery request,
    CancellationToken cancellationToken)
  {
    var stats = await unitOfWork.Betting
      .GetResearchPhaseSettledSummaryAsync(request.LeagueIds, cancellationToken)
      .ConfigureAwait(false);

    var winRate = stats.SettledSelectionsCount == 0
      ? 0m
      : (decimal)stats.WonSelectionsCount / stats.SettledSelectionsCount * 100m;
    var lossRate = stats.SettledSelectionsCount == 0
      ? 0m
      : (decimal)stats.LostSelectionsCount / stats.SettledSelectionsCount * 100m;

    return new AgentDashboardResearchBettingSummaryDto(
      stats.SettledSelectionsCount,
      stats.WonSelectionsCount,
      stats.LostSelectionsCount,
      winRate,
      lossRate);
  }
}
