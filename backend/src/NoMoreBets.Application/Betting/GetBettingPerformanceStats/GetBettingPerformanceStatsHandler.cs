using MediatR;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Betting.GetBettingPerformanceStats;

public record GetBettingPerformanceStatsQuery : IRequest<BettingPerformanceStatsDto>;

public sealed class GetBettingPerformanceStatsHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetBettingPerformanceStatsQuery, BettingPerformanceStatsDto>
{
  public async Task<BettingPerformanceStatsDto> Handle(
    GetBettingPerformanceStatsQuery request,
    CancellationToken cancellationToken)
  {
    // ponytail: loads all betting-phase slips and aggregates in memory; fine for a single agent
    // betting daily (hundreds of slips). Move to SQL grouping if the slip count ever gets hot.
    var slips = await unitOfWork.Betting.GetBettingPhaseBetSlipsAsync(cancellationToken).ConfigureAwait(false);
    var settled = slips.Where(s => s.BetStatus is BetStatus.Won or BetStatus.Lost).ToList();

    var byOddsBand = settled
      .GroupBy(s => OddsBand(s.TotalOdds))
      .OrderBy(g => g.Min(s => s.TotalOdds))
      .Select(g => ToBucket(g.Key, g))
      .ToList();

    var byParlaySize = settled
      .GroupBy(s => s.Selections.Count == 1 ? "single" : $"{s.Selections.Count}-leg parlay")
      .OrderBy(g => g.First().Selections.Count)
      .Select(g => ToBucket(g.Key, g))
      .ToList();

    var byMarketType = settled
      .SelectMany(s => s.Selections)
      .Where(sel => sel.BetStatus is BetStatus.Won or BetStatus.Lost)
      .GroupBy(sel => BettingEventTypeDisplay.GetDisplayName(sel.BetEventType))
      .OrderByDescending(g => g.Count())
      .Select(g => new MarketPerformanceBucketDto(
        g.Key,
        g.Count(),
        Rate(g.Count(sel => sel.BetStatus == BetStatus.Won), g.Count()),
        (double)g.Average(sel => sel.OddsAtPlacement)))
      .ToList();

    var calibration = settled
      .Where(s => s.EstimatedWinProbability.HasValue)
      .GroupBy(s => ProbabilityBand(s.EstimatedWinProbability!.Value))
      .OrderBy(g => g.Min(s => s.EstimatedWinProbability!.Value))
      .Select(g => new CalibrationBucketDto(
        g.Key,
        g.Count(),
        (double)g.Average(s => s.EstimatedWinProbability!.Value),
        Rate(g.Count(s => s.BetStatus == BetStatus.Won), g.Count())))
      .ToList();

    return new BettingPerformanceStatsDto(
      ToBucket("all settled slips", settled),
      byOddsBand,
      byParlaySize,
      byMarketType,
      calibration);
  }

  private static PerformanceBucketDto ToBucket(string name, IEnumerable<BetSlip> slips)
  {
    var list = slips as IReadOnlyCollection<BetSlip> ?? slips.ToList();
    var staked = list.Sum(s => s.StakeAmount);
    var returned = list.Where(s => s.BetStatus == BetStatus.Won).Sum(s => s.PotentialPayout);
    return new PerformanceBucketDto(
      name,
      list.Count,
      staked,
      returned,
      staked == 0m ? 0m : Math.Round((returned - staked) / staked, 4),
      Rate(list.Count(s => s.BetStatus == BetStatus.Won), list.Count));
  }

  private static double Rate(int hits, int total) => total == 0 ? 0 : Math.Round((double)hits / total, 4);

  private static string OddsBand(decimal totalOdds) => totalOdds switch
  {
    < 1.5m => "odds < 1.5",
    < 2m => "odds 1.5-2.0",
    < 3m => "odds 2.0-3.0",
    < 5m => "odds 3.0-5.0",
    _ => "odds 5.0+",
  };

  private static string ProbabilityBand(decimal p) => p switch
  {
    < 0.2m => "0-20%",
    < 0.4m => "20-40%",
    < 0.6m => "40-60%",
    < 0.8m => "60-80%",
    _ => "80-100%",
  };
}
