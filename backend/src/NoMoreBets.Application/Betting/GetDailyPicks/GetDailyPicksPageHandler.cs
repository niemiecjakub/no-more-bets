using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Application.Betting.Common;

namespace NoMoreBets.Application.Betting.GetDailyPicks;

public record GetDailyPicksPageQuery(int Limit, DateOnly? AfterSlipDate)
  : IRequest<Paged<BetSlipListItemDto>>;

public sealed class GetDailyPicksPageHandler(IBettingRepository betting)
  : IRequestHandler<GetDailyPicksPageQuery, Paged<BetSlipListItemDto>>
{
  public async Task<Paged<BetSlipListItemDto>> Handle(
    GetDailyPicksPageQuery request,
    CancellationToken cancellationToken)
  {
    var page = await betting
      .GetDailyPickSlipsPageAsync(request.Limit, request.AfterSlipDate, cancellationToken)
      .ConfigureAwait(false);

    var items = BetSlipListItemMapper.ToListItems(page.Items);
    return PagedFactory.Create(
      items,
      page.HasMore,
      item => DateTime.SpecifyKind(
        (item.SlipDate ?? DateOnly.MinValue).ToDateTime(TimeOnly.MinValue),
        DateTimeKind.Utc),
      item => item.Id);
  }
}
