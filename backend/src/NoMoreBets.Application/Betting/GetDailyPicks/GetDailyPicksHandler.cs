using MediatR;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Application.Betting.Common;

namespace NoMoreBets.Application.Betting.GetDailyPicks;

public record GetDailyPicksQuery(DateOnly SlipDate) : IRequest<IReadOnlyList<BetSlipListItemDto>>;

public sealed class GetDailyPicksHandler(IBettingRepository betting)
  : IRequestHandler<GetDailyPicksQuery, IReadOnlyList<BetSlipListItemDto>>
{
  public async Task<IReadOnlyList<BetSlipListItemDto>> Handle(
    GetDailyPicksQuery request,
    CancellationToken cancellationToken)
  {
    var slips = await betting
      .GetBetSlipsWithDailyPickOnDateAsync(request.SlipDate, cancellationToken)
      .ConfigureAwait(false);

    return BetSlipListItemMapper.ToListItems(slips);
  }
}
