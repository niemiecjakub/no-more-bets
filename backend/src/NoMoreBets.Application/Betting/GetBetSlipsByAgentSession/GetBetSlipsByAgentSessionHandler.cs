using MediatR;
using NoMoreBets.Application.Betting.Common;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Betting.GetBetSlipsByAgentSession;

public record GetBetSlipsByAgentSessionQuery(int SessionId)
  : IRequest<IReadOnlyList<BetSlipListItemDto>?>;

public sealed class GetBetSlipsByAgentSessionHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetBetSlipsByAgentSessionQuery, IReadOnlyList<BetSlipListItemDto>?>
{
  public async Task<IReadOnlyList<BetSlipListItemDto>?> Handle(
    GetBetSlipsByAgentSessionQuery request,
    CancellationToken cancellationToken)
  {
    if (!await unitOfWork.AgentSessions.SessionExistsAsync(request.SessionId, cancellationToken).ConfigureAwait(false))
    {
      return null;
    }

    var slips = await unitOfWork.Betting
      .GetBetSlipsByAgentSessionIdAsync(request.SessionId, cancellationToken)
      .ConfigureAwait(false);

    return BetSlipListItemMapper.ToListItems(slips);
  }
}
