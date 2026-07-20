using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Betting.Common;

public static class BetSlipListItemMapper
{
  public static IReadOnlyList<BetSlipListItemDto> ToListItems(IEnumerable<BetSlip> slips) =>
    slips.Select(ToListItem).ToList();

  public static BetSlipListItemDto ToListItem(BetSlip slip) =>
    new(
      slip.Id,
      slip.CreatedAt,
      slip.StakeAmount,
      slip.TotalOdds,
      slip.PotentialPayout,
      slip.StatusId,
      slip.BetStatusEntity.Name,
      slip.Selections
        .OrderBy(sel => sel.Id)
        .Select(sel => new BetSelectionItemDto(
          sel.MatchId,
          sel.Match.HomeClub.Name,
          sel.Match.AwayClub.Name,
          sel.Match.HomeClub.Slug,
          sel.Match.AwayClub.Slug,
          BettingEventTypeDisplay.GetDisplayName(sel.BetEventType),
          BettingEventOptionDisplay.GetDisplayName(
            sel.BetEventOption,
            sel.Match.HomeClub.Name,
            sel.Match.AwayClub.Name),
          sel.OddsAtPlacement,
          sel.StatusId,
          sel.BetStatusEntity.Name))
        .ToList(),
      slip.AgentSessionId,
      slip.Rationale,
      slip.EstimatedWinProbability);
}
