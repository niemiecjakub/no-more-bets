using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Application.Betting.Common;

namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardBettingSummarySlips;

public record GetAgentDashboardBettingSummarySlipsQuery(
  int Limit,
  DateTime? AfterCreatedAtUtc,
  int? AfterId,
  IReadOnlyList<string> SeasonYears) : IRequest<Paged<BetSlipListItemDto>>;

public sealed class GetAgentDashboardBettingSummarySlipsHandler(IBettingRepository betting)
  : IRequestHandler<GetAgentDashboardBettingSummarySlipsQuery, Paged<BetSlipListItemDto>>
{
  public async Task<Paged<BetSlipListItemDto>> Handle(
    GetAgentDashboardBettingSummarySlipsQuery request,
    CancellationToken cancellationToken)
  {
    var page = await betting
      .GetSettledBettingSlipIdsPageAsync(
        request.Limit,
        request.AfterCreatedAtUtc,
        request.AfterId,
        request.SeasonYears,
        cancellationToken)
      .ConfigureAwait(false);

    if (page.SlipIds.Count == 0)
    {
      return new Paged<BetSlipListItemDto>(Array.Empty<BetSlipListItemDto>(), false, null, null);
    }

    var slips = await betting
      .GetBettingPhaseBetSlipsByIdsAsync(page.SlipIds, cancellationToken)
      .ConfigureAwait(false);

    var items = BetSlipListItemMapper.ToListItems(slips);
    return PagedFactory.Create(items, page.HasMore, item => item.CreatedAt, item => item.Id);
  }
}
