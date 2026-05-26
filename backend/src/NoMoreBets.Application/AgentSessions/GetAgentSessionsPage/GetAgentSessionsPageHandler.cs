using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;

namespace NoMoreBets.Application.AgentSessions.GetAgentSessionsPage;

public record GetAgentSessionsPageQuery(
  int Limit,
  DateTime? AfterStartedAtUtc,
  int? AfterId,
  int? IncludeSessionId,
  IReadOnlyCollection<AgentSessionPhase>? PhaseIds = null) : IRequest<Paged<AgentSessionListItemDto>>;

public sealed class GetAgentSessionsPageHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetAgentSessionsPageQuery, Paged<AgentSessionListItemDto>>
{
  public async Task<Paged<AgentSessionListItemDto>> Handle(
    GetAgentSessionsPageQuery request,
    CancellationToken cancellationToken)
  {
    var page = await unitOfWork.AgentSessions
      .GetSessionsPageAsync(
        request.Limit,
        request.AfterStartedAtUtc,
        request.AfterId,
        request.IncludeSessionId,
        request.PhaseIds,
        cancellationToken)
      .ConfigureAwait(false);

    var matchIdBySessionId = await unitOfWork.AgentSessions
      .GetMatchIdsBySessionIdsAsync(page.Items.Select(r => r.Id).ToList(), cancellationToken)
      .ConfigureAwait(false);

    var items = page.Items
      .Select(r => new AgentSessionListItemDto(
        r.Id,
        (int)r.Phase,
        r.Phase.ToString(),
        r.StartedAt,
        r.MessageCount,
        matchIdBySessionId.TryGetValue(r.Id, out var matchId) ? matchId : null))
      .ToList();

    return PagedFactory.Create(items, page.HasMore, item => item.StartedAt, item => item.Id);
  }
}
