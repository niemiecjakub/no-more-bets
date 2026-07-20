using MediatR;
using NoMoreBets.Application.Betting.Common;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Bankroll.GetBankrollEntryBetDetails;

public record GetBankrollEntryBetDetailsQuery(int EntryId) : IRequest<BankrollEntryBetDetailsDto?>;

public sealed class GetBankrollEntryBetDetailsHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetBankrollEntryBetDetailsQuery, BankrollEntryBetDetailsDto?>
{
  public async Task<BankrollEntryBetDetailsDto?> Handle(
    GetBankrollEntryBetDetailsQuery request,
    CancellationToken cancellationToken)
  {
    var slip = await unitOfWork.Bankroll
      .GetBettingPhaseBetSlipForEntryAsync(request.EntryId, cancellationToken)
      .ConfigureAwait(false);

    if (slip is null)
      return null;

    var listItem = BetSlipListItemMapper.ToListItem(slip);

    return new BankrollEntryBetDetailsDto(
      request.EntryId,
      listItem.Id,
      listItem.CreatedAt,
      listItem.StakeAmount,
      listItem.TotalOdds,
      listItem.PotentialPayout,
      listItem.StatusId,
      listItem.StatusName,
      listItem.AgentSessionId,
      listItem.Selections,
      listItem.Rationale,
      listItem.EstimatedWinProbability);
  }
}
