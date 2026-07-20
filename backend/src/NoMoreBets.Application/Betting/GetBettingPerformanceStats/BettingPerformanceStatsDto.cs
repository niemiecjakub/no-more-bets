namespace NoMoreBets.Application.Betting.GetBettingPerformanceStats;

/// <summary>Aggregated results for a group of settled bet slips. Roi = (returned - staked) / staked.</summary>
public record PerformanceBucketDto(
  string Bucket,
  int SlipCount,
  decimal TotalStaked,
  decimal TotalReturned,
  decimal Roi,
  double HitRate);

/// <summary>Settled selections grouped by market type; hit rate is per selection, not per slip.</summary>
public record MarketPerformanceBucketDto(
  string MarketType,
  int SelectionCount,
  double HitRate,
  double AverageOdds);

/// <summary>Agent's average estimated win probability vs the actual win rate for slips in this band.</summary>
public record CalibrationBucketDto(
  string ProbabilityBand,
  int SlipCount,
  double AverageEstimatedProbability,
  double ActualWinRate);

/// <summary>Aggregated performance across all settled betting-phase slips.</summary>
public record BettingPerformanceStatsDto(
  PerformanceBucketDto Overall,
  IReadOnlyList<PerformanceBucketDto> ByOddsBand,
  IReadOnlyList<PerformanceBucketDto> ByParlaySize,
  IReadOnlyList<MarketPerformanceBucketDto> ByMarketType,
  IReadOnlyList<CalibrationBucketDto> Calibration);
