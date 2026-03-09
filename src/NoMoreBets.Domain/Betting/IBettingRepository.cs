namespace NoMoreBets.Domain.Betting;

public interface IBettingRepository
{
  Task<IReadOnlyList<BettingOddsSnapshot>> GetBettingOddsSnapshotsForMatchAsync(int matchId, CancellationToken cancellationToken = default);
}
