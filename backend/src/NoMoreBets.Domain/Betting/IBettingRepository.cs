using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Domain.Betting;

public interface IBettingRepository
{
  Task<IReadOnlyList<BettingOddsSnapshot>> GetBettingOddsSnapshotsForMatchAsync(int matchId, CancellationToken cancellationToken = default);
  Task<decimal?> GetCurrentOddsForSelectionAsync(int matchId, BettingEventType eventType, BettingEventOption eventOption, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<Match>> GetMatchesAvailableForBettingAsync(CancellationToken cancellationToken = default);
  Task AddBetSlipAsync(BetSlip slip, CancellationToken cancellationToken = default);
  Task<BetSlip?> GetBetSlipWithSelectionsByIdAsync(int betSlipId, CancellationToken cancellationToken = default);
  /// <summary>
  /// Bet slips from betting (and similar) sessions; excludes slips tied to <see cref="AgentSessionPhase.Research"/>.
  /// </summary>
  Task<IReadOnlyList<BetSlip>> GetBetSlipsAsync(BetStatus? slipStatus = null, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BetSlip>> GetBettingPhaseBetSlipsAsync(CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BetSlip>> GetBetSlipsByAgentSessionIdAsync(
    int agentSessionId,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Latest research-phase bet slip that includes a selection on <paramref name="matchId"/>, if any.
  /// </summary>
  Task<BetSlip?> GetLatestResearchBetSlipForMatchAsync(int matchId, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BetSlip>> GetNonPendingBetSlipsCreatedInLastDaysAsync(int lastDays, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BetSlip>> GetNonPendingBetSlipsUpdatedInLastDaysAsync(int lastDays, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BetSlip>> GetNonPendingBetSlipsAwaitingReflectionAsync(CancellationToken cancellationToken = default);
  Task MarkBetSlipsAgentSessionReflectedAsync(int agentSessionId, IReadOnlyList<int> betSlipIds, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BetSelection>> GetPendingSelectionsWithBothScoresAsync(CancellationToken cancellationToken = default);
  Task<IReadOnlySet<int>> GetMatchIdsWithResearchPhaseSelectionsAsync(
    IReadOnlyCollection<int> matchIds,
    CancellationToken cancellationToken = default);
  Task<BettingPhaseSummaryStats> GetBettingPhaseSettledSummaryAsync(CancellationToken cancellationToken = default);
  Task<ResearchPhaseSummaryStats> GetResearchPhaseSettledSummaryAsync(
    IReadOnlyList<int> leagueIds,
    CancellationToken cancellationToken = default);
  Task<BettingPhaseDetailCounts> GetBettingPhaseSettledDetailCountsAsync(CancellationToken cancellationToken = default);
  Task<BetSlipIdPage> GetSettledBettingSlipIdsPageAsync(
    int limit,
    DateTime? afterCreatedAtUtc,
    int? afterId,
    CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BetSlip>> GetBettingPhaseBetSlipsByIdsAsync(
    IReadOnlyList<int> slipIds,
    CancellationToken cancellationToken = default);
  Task<PendingBetsWidgetData> GetBettingPhasePendingBetsWidgetAsync(CancellationToken cancellationToken = default);
  Task<ClubBetSelectionStats> GetResearchPhaseSettledSelectionStatsForClubAsync(
    int clubId,
    CancellationToken cancellationToken = default);
}
