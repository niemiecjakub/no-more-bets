using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using BankrollEntry = NoMoreBets.Domain.Bankrolls.Bankroll;

namespace NoMoreBets.Application.Betting.SettlePendingBetSelections;

public record SettlePendingBetSelectionsCommand : IRequest<Unit>;

public sealed class SettlePendingBetSelectionsHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<SettlePendingBetSelectionsCommand, Unit>
{
  public async Task<Unit> Handle(
    SettlePendingBetSelectionsCommand request,
    CancellationToken cancellationToken)
  {
    var pendingWithScores = await unitOfWork.Betting
      .GetPendingSelectionsWithBothScoresAsync(cancellationToken)
      .ConfigureAwait(false);

    if (pendingWithScores.Count == 0)
    {
      return Unit.Value;
    }

    foreach (var selection in pendingWithScores)
    {
      if (selection.BetStatus != BetStatus.Pending)
      {
        continue;
      }

      var home = selection.Match.HomeGoals!.Value;
      var away = selection.Match.AwayGoals!.Value;
      selection.BetStatus = BettingSelectionOutcomeEvaluator.ResolveBetStatus(
        selection.BetEventOption,
        home,
        away);
    }

    var slipsToRollup = pendingWithScores
      .Select(s => s.BetSlip)
      .DistinctBy(slip => slip.Id)
      .ToList();

    foreach (var slip in slipsToRollup)
    {
      var previous = slip.BetStatus;
      var next = slip.ComputeStatusFromSelections();
      if (next == previous)
      {
        continue;
      }

      slip.BetStatus = next;

      if (previous == BetStatus.Pending && next == BetStatus.Won)
      {
        var payout = BankrollEntry.CreateBetWin(slip.PotentialPayout, slip.Id);
        await unitOfWork.Bankroll.AddAsync(payout, cancellationToken).ConfigureAwait(false);
      }
    }

    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    return Unit.Value;
  }
}
