using System.ComponentModel;

namespace NoMoreBets.Application.Betting.GetMatchBettingOddsHistory;

public record MarketPriceHistory(
  [Description("The unique identifier for the market type")] string MarketKey,
  [Description("The human-readable name of the market, e.g., 'Total Goals'")] string? MarketDisplayName,
  IReadOnlyList<OutcomePriceTimeline> Outcomes);

public record OutcomePriceTimeline(
  [Description("The name of the specific outcome")] string OutcomeName,
  [Description("The historical progression of prices, sorted by date")] IReadOnlyList<PricePoint> Timeline);

public record PricePoint(
  [Description("The decimal odds value")] double Price,
  [Description("The timestamp when this price became live")] DateTime Timestamp);
