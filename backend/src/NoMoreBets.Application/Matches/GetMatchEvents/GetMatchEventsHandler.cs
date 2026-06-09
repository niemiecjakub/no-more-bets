using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Matches.GetMatchEvents;

public record GetMatchEventsQuery(int MatchId) : IRequest<IReadOnlyList<MatchEventDto>>;

public sealed class GetMatchEventsHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetMatchEventsQuery, IReadOnlyList<MatchEventDto>>
{
  public async Task<IReadOnlyList<MatchEventDto>> Handle(
    GetMatchEventsQuery request,
    CancellationToken cancellationToken)
  {
    var events = await unitOfWork.Matches
      .GetMatchEventsForMatchAsync(request.MatchId, cancellationToken)
      .ConfigureAwait(false);

    return events
      .OrderBy(e => e.Minute)
      .ThenBy(e => e.Id)
      .Select(e => new MatchEventDto(
        e.Player.Name,
        e.ClubId,
        e.EventTypeId,
        e.EventTypeEntity.Name,
        e.Minute))
      .ToList();
  }
}
