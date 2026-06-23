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

    var matchSummaryBySessionId = await unitOfWork.AgentSessions
      .GetMatchSummariesBySessionIdsAsync(page.Items.Select(r => r.Id).ToList(), cancellationToken)
      .ConfigureAwait(false);

    var items = page.Items
      .Select(r =>
      {
        matchSummaryBySessionId.TryGetValue(r.Id, out var matchSummary);
        return new AgentSessionListItemDto(
          r.Id,
          (int)r.Phase,
          r.Phase.ToString(),
          r.StartedAt,
          r.MessageCount,
          matchSummary?.MatchId,
          matchSummary is null
            ? null
            : new AgentSessionMatchSummaryDto(
              matchSummary.MatchId,
              matchSummary.HomeClubName,
              matchSummary.AwayClubName,
              matchSummary.HomeClubSlug,
              matchSummary.AwayClubSlug,
              matchSummary.MatchDate,
              matchSummary.MatchStatusId,
              matchSummary.HomeGoals,
              matchSummary.AwayGoals));
      })
      .ToList();

    return PagedFactory.Create(items, page.HasMore, item => item.StartedAt, item => item.Id);
  }
}
