using MediatR;
using NoMoreBets.Domain.Betting;

namespace NoMoreBets.Application.Betting.GetBetSlips;

/// <param name="LastDays">Rolling UTC window: slips with CreatedAt on or after UtcNow minus this many days.</param>
public record GetNonPendingBetSlipsRecentQuery(int LastDays) : IRequest<IReadOnlyList<BetSlipSummary>>;

public sealed class GetNonPendingBetSlipsRecentHandler(IBettingRepository betting)
  : IRequestHandler<GetNonPendingBetSlipsRecentQuery, IReadOnlyList<BetSlipSummary>>
{
  public async Task<IReadOnlyList<BetSlipSummary>> Handle(
    GetNonPendingBetSlipsRecentQuery request,
    CancellationToken cancellationToken)
  {
    var slips = await betting
      .GetNonPendingBetSlipsCreatedInLastDaysAsync(request.LastDays, cancellationToken)
      .ConfigureAwait(false);

    return BetSlipSummaryMapper.ToSummaries(slips);
  }
}
