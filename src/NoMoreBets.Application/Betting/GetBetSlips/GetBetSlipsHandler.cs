using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Betting.GetBetSlips;

/// <param name="Status">When set, only slips with this status are returned; when omitted, all slips are returned.</param>
public record GetBetSlipsQuery(BetStatus? Status = null) : IRequest<IReadOnlyList<BetSlipSummary>>;

public sealed class GetBetSlipsHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetBetSlipsQuery, IReadOnlyList<BetSlipSummary>>
{
  public async Task<IReadOnlyList<BetSlipSummary>> Handle(
    GetBetSlipsQuery request,
    CancellationToken cancellationToken)
  {
    var slips = await unitOfWork.Betting
      .GetBetSlipsAsync(request.Status, cancellationToken)
      .ConfigureAwait(false);

    return BetSlipSummaryMapper.ToSummaries(slips);
  }
}
