using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardMemories;

public record GetAgentDashboardMemoriesQuery : IRequest<AgentDashboardMemoriesDto>;

public sealed class GetAgentDashboardMemoriesHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetAgentDashboardMemoriesQuery, AgentDashboardMemoriesDto>
{
  public async Task<AgentDashboardMemoriesDto> Handle(
    GetAgentDashboardMemoriesQuery request,
    CancellationToken cancellationToken)
  {
    var data = await unitOfWork.Memories
      .GetActiveMemoriesWidgetAsync(cancellationToken)
      .ConfigureAwait(false);

    return new AgentDashboardMemoriesDto(
      data.MemoriesCount,
      data.LatestUpdatedAt,
      data.LatestName);
  }
}
