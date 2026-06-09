using MediatR;
using NoMoreBets.Application.Betting.Common;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Betting.GetBetSlipsList;

public record GetBetSlipsListQuery : IRequest<IReadOnlyList<BetSlipListItemDto>>;

public sealed class GetBetSlipsListHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetBetSlipsListQuery, IReadOnlyList<BetSlipListItemDto>>
{
  public async Task<IReadOnlyList<BetSlipListItemDto>> Handle(
    GetBetSlipsListQuery request,
    CancellationToken cancellationToken)
  {
    var slips = await unitOfWork.Betting
      .GetBettingPhaseBetSlipsAsync(cancellationToken)
      .ConfigureAwait(false);

    return BetSlipListItemMapper.ToListItems(slips);
  }
}
