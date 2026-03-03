using MediatR;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Betting.Dto;

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
