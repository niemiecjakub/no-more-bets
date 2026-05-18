using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardSessions;

public record GetAgentDashboardSessionsQuery : IRequest<AgentDashboardSessionsDto>;

public sealed class GetAgentDashboardSessionsHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetAgentDashboardSessionsQuery, AgentDashboardSessionsDto>
{
  public async Task<AgentDashboardSessionsDto> Handle(
    GetAgentDashboardSessionsQuery request,
    CancellationToken cancellationToken)
  {
    var data = await unitOfWork.AgentSessions
      .GetSessionsWidgetAsync(cancellationToken)
      .ConfigureAwait(false);

    return new AgentDashboardSessionsDto(
      data.SessionsCount,
      data.LatestStartedAt,
      data.LatestPhaseName);
  }
}
