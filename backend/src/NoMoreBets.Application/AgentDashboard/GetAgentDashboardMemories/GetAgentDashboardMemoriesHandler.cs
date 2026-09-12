using MediatR;
using NoMoreBets.Domain.Memories;

namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardMemories;

public record GetAgentDashboardMemoriesQuery : IRequest<AgentDashboardMemoriesDto>;

public sealed class GetAgentDashboardMemoriesHandler(IMemoryRepository memories)
  : IRequestHandler<GetAgentDashboardMemoriesQuery, AgentDashboardMemoriesDto>
{
  public async Task<AgentDashboardMemoriesDto> Handle(
    GetAgentDashboardMemoriesQuery request,
    CancellationToken cancellationToken)
  {
    var data = await memories
      .GetActiveMemoriesWidgetAsync(cancellationToken)
      .ConfigureAwait(false);

    return new AgentDashboardMemoriesDto(
      data.MemoriesCount,
      data.LatestUpdatedAt,
      data.LatestName);
  }
}
