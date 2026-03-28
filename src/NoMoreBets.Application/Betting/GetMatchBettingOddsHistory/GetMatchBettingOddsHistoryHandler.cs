using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Betting.GetMatchBettingOddsHistory;

public record GetMatchBettingOddsHistoryQuery(int MatchId) : IRequest<IReadOnlyList<MarketPriceHistory>?>;

public sealed class GetMatchBettingOddsHistoryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMatchBettingOddsHistoryQuery, IReadOnlyList<MarketPriceHistory>?>
{
  public async Task<IReadOnlyList<MarketPriceHistory>?> Handle(GetMatchBettingOddsHistoryQuery request, CancellationToken cancellationToken)
  {
    var snapshots = await unitOfWork.Betting.GetBettingOddsSnapshotsForMatchAsync(request.MatchId, cancellationToken).ConfigureAwait(false);

    if (snapshots.Count == 0)
      return null;

    var byEventType = new Dictionary<int, EventTypeOddsAccumulator>();

    foreach (var snapshot in snapshots)
    {
      foreach (var row in snapshot.Rows)
      {
        var eventType = (BettingEventType)row.EventTypeId;
        if (!BettingOddsHistoryEventTypeWhitelist.Contains(eventType))
          continue;

        if (!byEventType.TryGetValue(row.EventTypeId, out var acc))
        {
          acc = new EventTypeOddsAccumulator { EventTypeName = row.EventTypeEntity.Name };
          byEventType[row.EventTypeId] = acc;
        }

        var outcomeName = row.EventOptionEntity?.Name;
        if (string.IsNullOrEmpty(outcomeName) || !row.Odds.HasValue)
          continue;

        if (acc.Title == null)
          acc.Title = row.EventTypeEntity.Name;

        if (!acc.OptionOrder.Contains(outcomeName))
          acc.OptionOrder.Add(outcomeName);

        if (!acc.OddsByLabel.TryGetValue(outcomeName, out var list))
        {
          list = new List<(double Odds, DateTime At)>();
          acc.OddsByLabel[outcomeName] = list;
        }

        list.Add(((double)row.Odds.Value, snapshot.SnapshotTime));
      }
    }

    return byEventType.Select(kv =>
    {
      var acc = kv.Value;
      var options = acc.OptionOrder.Select(label =>
      {
        var segments = CollapseToSegments(acc.OddsByLabel.TryGetValue(label, out var o) ? o : Array.Empty<(double, DateTime)>());
        return new OutcomePriceTimeline(label, segments);
      }).ToList();
      return new MarketPriceHistory(acc.EventTypeName, acc.Title, options);
    }).ToList();
  }

  private static IReadOnlyList<PricePoint> CollapseToSegments(IReadOnlyList<(double Odds, DateTime At)> points)
  {
    if (points.Count == 0)
      return Array.Empty<PricePoint>();

    var sorted = points.OrderBy(p => p.At).ToList();
    var segments = new List<PricePoint> { new(sorted[0].Odds, sorted[0].At) };

    foreach (var point in sorted.Skip(1))
    {
      if (point.Odds != segments[^1].Price)
        segments.Add(new PricePoint(point.Odds, point.At));
    }

    return segments;
  }

  private static readonly HashSet<BettingEventType> BettingOddsHistoryEventTypeWhitelist =
  [
    BettingEventType.MatchResult,
    BettingEventType.DoubleChance,
    BettingEventType.OverUnderGoals,
    BettingEventType.BothTeamsToScore,
    BettingEventType.TeamGoals,
    BettingEventType.Handicap,
    BettingEventType.ExactScore
  ];

  private sealed class EventTypeOddsAccumulator
  {
    public string EventTypeName { get; set; } = "";
    public string? Title { get; set; }
    public List<string> OptionOrder { get; set; } = [];
    public Dictionary<string, List<(double Odds, DateTime At)>> OddsByLabel { get; set; } = new(StringComparer.Ordinal);
  }
}
