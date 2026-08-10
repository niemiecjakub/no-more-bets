using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardBettingSummaryDetails;

public record GetAgentDashboardBettingSummaryDetailsQuery(IReadOnlyList<string> SeasonYears)
  : IRequest<AgentDashboardBettingSummaryDetailsDto>;

public sealed class GetAgentDashboardBettingSummaryDetailsHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetAgentDashboardBettingSummaryDetailsQuery, AgentDashboardBettingSummaryDetailsDto>
{
  public async Task<AgentDashboardBettingSummaryDetailsDto> Handle(
    GetAgentDashboardBettingSummaryDetailsQuery request,
    CancellationToken cancellationToken)
  {
    var counts = await unitOfWork.Betting
      .GetBettingPhaseSettledDetailCountsAsync(request.SeasonYears, cancellationToken)
      .ConfigureAwait(false);

    return new AgentDashboardBettingSummaryDetailsDto(
      counts.WonSlipsCount,
      counts.LostSlipsCount,
      counts.WonSelectionsCount,
      counts.LostSelectionsCount);
  }
}
