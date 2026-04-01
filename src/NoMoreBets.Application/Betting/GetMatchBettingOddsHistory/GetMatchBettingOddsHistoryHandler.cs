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

    var match = await unitOfWork.Matches.GetMatchByIdAsync(request.MatchId, cancellationToken).ConfigureAwait(false);
    var homeName = match?.HomeClub?.Name;
    var awayName = match?.AwayClub?.Name;

    var byEventType = new Dictionary<int, EventTypeOddsAccumulator>();

    foreach (var snapshot in snapshots)
    {
      foreach (var row in snapshot.Rows)
      {
        if (!byEventType.TryGetValue(row.EventTypeId, out var acc))
        {
          acc = new EventTypeOddsAccumulator { EventTypeName = row.EventTypeEntity.Name };
          byEventType[row.EventTypeId] = acc;
        }

        var outcomeName = row.EventOptionEntity?.Name;
        if (string.IsNullOrEmpty(outcomeName) || !row.Odds.HasValue)
          continue;

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
      var eventType = (BettingEventType)kv.Key;
      var marketDisplayName = BettingEventTypeDisplay.GetDisplayName(eventType);
      var options = acc.OptionOrder.Select(label =>
      {
        var segments = CollapseToSegments(acc.OddsByLabel.TryGetValue(label, out var o) ? o : Array.Empty<(double, DateTime)>());
        var outcomeDisplay = Enum.TryParse<BettingEventOption>(label, ignoreCase: false, out var parsedOption)
          ? BettingEventOptionDisplay.GetDisplayName(parsedOption, homeName, awayName)
          : label;
        return new OutcomePriceTimeline(outcomeDisplay, segments);
      }).ToList();
      return new MarketPriceHistory(acc.EventTypeName, marketDisplayName, options);
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

  private sealed class EventTypeOddsAccumulator
  {
    public string EventTypeName { get; set; } = "";
    public List<string> OptionOrder { get; set; } = [];
    public Dictionary<string, List<(double Odds, DateTime At)>> OddsByLabel { get; set; } = new(StringComparer.Ordinal);
  }
}
