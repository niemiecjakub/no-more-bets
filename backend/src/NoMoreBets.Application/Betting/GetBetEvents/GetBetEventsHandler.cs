using MediatR;
using NoMoreBets.Application.Betting;
using NoMoreBets.Application.Common.Dto.Betting;

namespace NoMoreBets.Application.Betting.GetBetEvents;
public record GetBetclicMatchEventsQuery(string GameUrl, bool Expand = false) : IRequest<IReadOnlyList<BookmakerEvent>>;

public class GetBetEventsHandler(IBetEventsProvider betEventsProvider) : IRequestHandler<GetBetclicMatchEventsQuery, IReadOnlyList<BookmakerEvent>>
{
  /// <inheritdoc />
  public async Task<IReadOnlyList<BookmakerEvent>> Handle(GetBetclicMatchEventsQuery request, CancellationToken cancellationToken)
  {
    return await betEventsProvider.GetMatchEventsAsync(request.GameUrl, request.Expand, cancellationToken).ConfigureAwait(false);
  }
}
