using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Betting.GetMatchBettingOdds;

public record GetMatchBettingOddsQuery(int MatchId, bool IncludeExoticMarkets = false)
  : IRequest<IReadOnlyList<CurrentOddsMarket>>;

public sealed class GetMatchBettingOddsHandler(IUnitOfWork unitOfWork, ILogger<GetMatchBettingOddsHandler>? logger = null)
  : IRequestHandler<GetMatchBettingOddsQuery, IReadOnlyList<CurrentOddsMarket>>
{
  public async Task<IReadOnlyList<CurrentOddsMarket>> Handle(GetMatchBettingOddsQuery request, CancellationToken cancellationToken)
  {
    var snapshots = await unitOfWork.Betting.GetBettingOddsSnapshotsForMatchAsync(request.MatchId, cancellationToken).ConfigureAwait(false);

    if (snapshots.Count == 0)
    {
      logger?.LogWarning("No current odds snapshots found for match {MatchId}.", request.MatchId);
      return Array.Empty<CurrentOddsMarket>();
    }

    var latest = snapshots[0];
    var byEventType = new Dictionary<int, (string Name, List<CurrentOddsOption> Options)>();

    foreach (var row in latest.Rows)
    {
      if (row.EventTypeEntity is null)
      {
        logger?.LogWarning("Skipping odds row with missing event type entity for match {MatchId}. EventTypeId={EventTypeId}", request.MatchId, row.EventTypeId);
        continue;
      }

      if (!request.IncludeExoticMarkets && row.EventTypeId is not (
        (int)BettingEventType.OverUnderGoals
        or (int)BettingEventType.DoubleChance
        or (int)BettingEventType.BothTeamsToScore
        or (int)BettingEventType.MatchResult))
      {
        continue;
      }

      var outcomeName = row.EventOptionEntity?.Name;
      if (string.IsNullOrEmpty(outcomeName) || !row.Odds.HasValue)
      {
        logger?.LogWarning("Skipping odds row with incomplete data for match {MatchId}. EventTypeId={EventTypeId}", request.MatchId, row.EventTypeId);
        continue;
      }

      if (!byEventType.TryGetValue(row.EventTypeId, out var bucket))
      {
        bucket = (row.EventTypeEntity.Name, new List<CurrentOddsOption>());
        byEventType[row.EventTypeId] = bucket;
      }

      bucket.Options.Add(new CurrentOddsOption(outcomeName, (double)row.Odds.Value));
    }

    return byEventType
      .OrderBy(kv => kv.Key)
      .Select(kv => new CurrentOddsMarket(kv.Key, kv.Value.Name, kv.Value.Options))
      .ToList();
  }
}
