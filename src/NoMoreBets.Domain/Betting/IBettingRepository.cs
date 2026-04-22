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
  Task<IReadOnlyList<BetSlip>> GetBetSlipsAsync(BetStatus? slipStatus = null, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BetSlip>> GetNonPendingBetSlipsCreatedInLastDaysAsync(int lastDays, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BetSlip>> GetNonPendingBetSlipsUpdatedInLastDaysAsync(int lastDays, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BetSlip>> GetNonPendingBetSlipsAwaitingReflectionAsync(CancellationToken cancellationToken = default);
  Task MarkBetSlipsAgentSessionReflectedAsync(int agentSessionId, IReadOnlyList<int> betSlipIds, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BetSelection>> GetPendingSelectionsWithBothScoresAsync(CancellationToken cancellationToken = default);
}
