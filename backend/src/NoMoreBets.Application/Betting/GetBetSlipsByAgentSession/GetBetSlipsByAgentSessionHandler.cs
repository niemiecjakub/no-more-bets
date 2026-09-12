using MediatR;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Application.Betting.Common;

namespace NoMoreBets.Application.Betting.GetBetSlipsByAgentSession;

public record GetBetSlipsByAgentSessionQuery(int SessionId)
  : IRequest<IReadOnlyList<BetSlipListItemDto>?>;

public sealed class GetBetSlipsByAgentSessionHandler(IBettingRepository betting, IAgentSessionRepository agentSessions)
  : IRequestHandler<GetBetSlipsByAgentSessionQuery, IReadOnlyList<BetSlipListItemDto>?>
{
  public async Task<IReadOnlyList<BetSlipListItemDto>?> Handle(
    GetBetSlipsByAgentSessionQuery request,
    CancellationToken cancellationToken)
  {
    if (!await agentSessions.SessionExistsAsync(request.SessionId, cancellationToken).ConfigureAwait(false))
    {
      return null;
    }

    var slips = await betting
      .GetBetSlipsByAgentSessionIdAsync(request.SessionId, cancellationToken)
      .ConfigureAwait(false);

    return BetSlipListItemMapper.ToListItems(slips);
  }
}
