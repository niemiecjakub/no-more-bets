using MediatR;
using NoMoreBets.Application.Betting.Common;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Betting.GetDailyPicks;

public record GetDailyPicksQuery(DateOnly SlipDate) : IRequest<IReadOnlyList<BetSlipListItemDto>>;

public sealed class GetDailyPicksHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetDailyPicksQuery, IReadOnlyList<BetSlipListItemDto>>
{
  public async Task<IReadOnlyList<BetSlipListItemDto>> Handle(
    GetDailyPicksQuery request,
    CancellationToken cancellationToken)
  {
    var slips = await unitOfWork.Betting
      .GetBetSlipsWithDailyPickOnDateAsync(request.SlipDate, cancellationToken)
      .ConfigureAwait(false);

    return BetSlipListItemMapper.ToListItems(slips);
  }
}
