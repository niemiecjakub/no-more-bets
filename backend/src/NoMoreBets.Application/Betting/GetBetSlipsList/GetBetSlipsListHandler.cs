using MediatR;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Application.Betting.Common;

namespace NoMoreBets.Application.Betting.GetBetSlipsList;

public record GetBetSlipsListQuery(IReadOnlyList<string> SeasonYears)
  : IRequest<IReadOnlyList<BetSlipListItemDto>>;

public sealed class GetBetSlipsListHandler(IBettingRepository betting)
  : IRequestHandler<GetBetSlipsListQuery, IReadOnlyList<BetSlipListItemDto>>
{
  public async Task<IReadOnlyList<BetSlipListItemDto>> Handle(
    GetBetSlipsListQuery request,
    CancellationToken cancellationToken)
  {
    var slips = await betting
      .GetBettingPhaseBetSlipsAsync(request.SeasonYears, cancellationToken)
      .ConfigureAwait(false);

    return BetSlipListItemMapper.ToListItems(slips);
  }
}
