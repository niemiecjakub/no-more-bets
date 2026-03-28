using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Application.Common.Dto.Betting;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;

public class BettingRepository : IBettingRepository
{
  private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

  private readonly AppDbContext _db;

  public BettingRepository(AppDbContext db)
  {
    _db = db;
  }

  public async Task<IReadOnlyList<BettingOddsSnapshot>> GetBettingOddsSnapshotsForMatchAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _db.BettingOddsSnapshot
      .Where(s => s.MatchId == matchId)
      .Include(s => s.Rows)
      .ThenInclude(r => r.EventTypeEntity)
      .Include(s => s.Rows)
      .ThenInclude(r => r.EventOptionEntity)
      .OrderByDescending(s => s.SnapshotTime)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<decimal?> GetCurrentOddsForSelectionAsync(int matchId, BettingEventType eventType, string outcomeKey, CancellationToken cancellationToken = default)
  {
    var snapshots = await GetBettingOddsSnapshotsForMatchAsync(matchId, cancellationToken).ConfigureAwait(false);
    if (snapshots.Count == 0)
      return null;

    var latest = snapshots[0];
    var eventTypeId = (int)eventType;
    foreach (var row in latest.Rows.Where(r => r.EventTypeId == eventTypeId))
    {
      var ev = JsonSerializer.Deserialize<BookmakerEvent>(row.EventJson, SerializerOptions);
      if (ev == null)
        continue;

      var option = ev.Options.FirstOrDefault(o => string.Equals(o.Label, outcomeKey, StringComparison.Ordinal));
      if (option != null)
        return (decimal)option.Odds;
    }

    return null;
  }

  public async Task<IReadOnlyList<Match>> GetMatchesAvailableForBettingAsync(CancellationToken cancellationToken = default)
  {
    var matchIdsWithSnapshots = _db.BettingOddsSnapshot.Select(s => s.MatchId).Distinct();
    var matchIdsWithAnalysis = _db.MatchAnalysis.Select(a => a.MatchId).Distinct();

    return await _db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming
        && matchIdsWithSnapshots.Contains(m.Id)
        && matchIdsWithAnalysis.Contains(m.Id))
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task AddBetSlipAsync(BetSlip slip, CancellationToken cancellationToken = default)
  {
    await _db.BetSlip.AddAsync(slip, cancellationToken).ConfigureAwait(false);
  }
}
