using MediatR;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Application.Betting.Common;

namespace NoMoreBets.Application.Bankroll.GetBankrollEntryBetDetails;

public record GetBankrollEntryBetDetailsQuery(int EntryId) : IRequest<BankrollEntryBetDetailsDto?>;

public sealed class GetBankrollEntryBetDetailsHandler(IBankrollRepository bankroll)
  : IRequestHandler<GetBankrollEntryBetDetailsQuery, BankrollEntryBetDetailsDto?>
{
  public async Task<BankrollEntryBetDetailsDto?> Handle(
    GetBankrollEntryBetDetailsQuery request,
    CancellationToken cancellationToken)
  {
    var slip = await bankroll
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
