using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Betting.GetBetSlips;

public static class BetSlipSummaryMapper
{
  public static IReadOnlyList<BetSlipSummary> ToSummaries(IEnumerable<BetSlip> slips) =>
    slips.Select(ToSummary).ToList();

  public static BetSlipSummary? ToSummaryOrNull(BetSlip? slip) =>
    slip is null ? null : ToSummary(slip);

  private static BetSlipSummary ToSummary(BetSlip s) =>
    new(
      s.Id,
      s.CreatedAt,
      s.StakeAmount,
      s.TotalOdds,
      s.PotentialPayout,
      s.BetStatus,
      s.Selections
        .OrderBy(sel => sel.Id)
        .Select(sel => new BetSelectionSummary(
          sel.MatchId,
          sel.Match.HomeClub.Name,
          sel.Match.AwayClub.Name,
          BettingEventTypeDisplay.GetDisplayName(sel.BetEventType),
          BettingEventOptionDisplay.GetDisplayName(
            sel.BetEventOption,
            sel.Match.HomeClub.Name,
            sel.Match.AwayClub.Name),
          sel.OddsAtPlacement,
          sel.BetStatus))
        .ToList(),
      s.Rationale,
      s.EstimatedWinProbability);
}
