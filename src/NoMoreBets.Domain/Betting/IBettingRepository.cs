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

  /// <summary>
  /// Paper / prediction slips created during match research (<see cref="AgentSessionPhase.Research"/> sessions only).
  /// </summary>
  Task<IReadOnlyList<BetSlip>> GetResearchPhaseBetSlipsAsync(BetStatus? slipStatus = null, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BetSlip>> GetNonPendingBetSlipsCreatedInLastDaysAsync(int lastDays, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BetSlip>> GetNonPendingBetSlipsUpdatedInLastDaysAsync(int lastDays, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BetSlip>> GetNonPendingBetSlipsAwaitingReflectionAsync(CancellationToken cancellationToken = default);
  Task MarkBetSlipsAgentSessionReflectedAsync(int agentSessionId, IReadOnlyList<int> betSlipIds, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BetSelection>> GetPendingSelectionsWithBothScoresAsync(CancellationToken cancellationToken = default);
}
