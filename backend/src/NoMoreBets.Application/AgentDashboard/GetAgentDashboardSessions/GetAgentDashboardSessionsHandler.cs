using MediatR;
using NoMoreBets.Domain.AgentSessions;

namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardSessions;

public record GetAgentDashboardSessionsQuery(IReadOnlyList<string> SeasonYears)
  : IRequest<AgentDashboardSessionsDto>;

public sealed class GetAgentDashboardSessionsHandler(IAgentSessionRepository agentSessions)
  : IRequestHandler<GetAgentDashboardSessionsQuery, AgentDashboardSessionsDto>
{
  public async Task<AgentDashboardSessionsDto> Handle(
    GetAgentDashboardSessionsQuery request,
    CancellationToken cancellationToken)
  {
    var data = await agentSessions
      .GetSessionsWidgetAsync(request.SeasonYears, cancellationToken)
      .ConfigureAwait(false);

    return new AgentDashboardSessionsDto(
      data.SessionsCount,
      data.LatestStartedAt,
      data.LatestPhaseName);
  }
}
