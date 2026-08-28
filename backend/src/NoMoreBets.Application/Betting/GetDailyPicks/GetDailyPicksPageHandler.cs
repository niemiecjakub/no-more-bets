using MediatR;
using NoMoreBets.Application.Betting.Common;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Betting.GetDailyPicks;

public record GetDailyPicksPageQuery(int Limit, DateOnly? AfterSlipDate)
  : IRequest<Paged<BetSlipListItemDto>>;

public sealed class GetDailyPicksPageHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetDailyPicksPageQuery, Paged<BetSlipListItemDto>>
{
  public async Task<Paged<BetSlipListItemDto>> Handle(
    GetDailyPicksPageQuery request,
    CancellationToken cancellationToken)
  {
    var page = await unitOfWork.Betting
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
