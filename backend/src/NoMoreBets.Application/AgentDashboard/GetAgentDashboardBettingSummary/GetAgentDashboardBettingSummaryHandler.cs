using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardBettingSummary;

public record GetAgentDashboardBettingSummaryQuery(IReadOnlyList<string> SeasonYears)
  : IRequest<AgentDashboardBettingSummaryDto>;

public sealed class GetAgentDashboardBettingSummaryHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetAgentDashboardBettingSummaryQuery, AgentDashboardBettingSummaryDto>
{
  public async Task<AgentDashboardBettingSummaryDto> Handle(
    GetAgentDashboardBettingSummaryQuery request,
    CancellationToken cancellationToken)
  {
    var stats = await unitOfWork.Betting
      .GetBettingPhaseSettledSummaryAsync(request.SeasonYears, cancellationToken)
      .ConfigureAwait(false);

    var winRate = stats.SettledSlipsCount == 0 ? 0m : (decimal)stats.WonSlipsCount / stats.SettledSlipsCount * 100m;
    var lossRate = stats.SettledSlipsCount == 0 ? 0m : (decimal)stats.LostSlipsCount / stats.SettledSlipsCount * 100m;

    return new AgentDashboardBettingSummaryDto(
      stats.SettledSlipsCount,
      stats.SettledSelectionsCount,
      stats.WonSlipsCount,
      stats.LostSlipsCount,
      winRate,
      lossRate);
  }
}
