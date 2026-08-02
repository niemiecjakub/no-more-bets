using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Domain.Betting;

public sealed record BettingPhaseSummaryStats(
  int SettledSlipsCount,
  int SettledSelectionsCount,
  int WonSlipsCount,
  int LostSlipsCount);

public sealed record ResearchPhaseSummaryStats(
  int SettledSelectionsCount,
  int WonSelectionsCount,
  int LostSelectionsCount);

/// <summary>Lightweight leg row for aggregating research-slip Parlay vs Singles P&amp;L.</summary>
public sealed record ResearchPhaseScenarioLegRow(
  int SlipId,
  decimal OddsAtPlacement,
  BetStatus Status);

public sealed record BettingPhaseDetailCounts(
  int WonSlipsCount,
  int LostSlipsCount,
  int WonSelectionsCount,
  int LostSelectionsCount);

public sealed record BetSlipIdPage(IReadOnlyList<int> SlipIds, bool HasMore);

public sealed record PendingBetsWidgetData(
  int PendingSlipsCount,
  decimal PendingStakeTotal,
  decimal PendingPotentialPayoutTotal,
  DateTime? LatestPendingCreatedAt);

public sealed record ClubBetSelectionStats(
  int WonCount,
  int LostCount,
  int TotalCount);
