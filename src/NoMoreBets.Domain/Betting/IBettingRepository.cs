using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Domain.Betting;

public interface IBettingRepository
{
  Task<IReadOnlyList<BettingOddsSnapshot>> GetBettingOddsSnapshotsForMatchAsync(int matchId, CancellationToken cancellationToken = default);
  Task<decimal?> GetCurrentOddsForSelectionAsync(int matchId, BettingEventType eventType, BettingEventOption eventOption, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<Match>> GetMatchesAvailableForBettingAsync(CancellationToken cancellationToken = default);
  Task AddBetSlipAsync(BetSlip slip, CancellationToken cancellationToken = default);
  /// <param name="slipStatus">When set, only slips with this status; when null, all slips.</param>
  Task<IReadOnlyList<BetSlip>> GetBetSlipsAsync(BetStatus? slipStatus = null, CancellationToken cancellationToken = default);
}
