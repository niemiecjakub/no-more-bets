using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Domain.Betting;

/// <summary>
/// Hypothetical P&amp;L for a research slip under equal-total-stake parlay vs singles scenarios.
/// Unit stake is fixed so N singles risk the same capital as one N-leg parlay.
/// </summary>
public static class ResearchBetScenarioCalculator
{
  public const decimal UnitStake = 5m;

  public static ResearchBetScenarioResult Calculate(IReadOnlyList<ResearchBetScenarioLegInput> legs)
  {
    var active = legs.Where(l => l.Status != BetStatus.Canceled).ToList();
    if (active.Count == 0)
    {
      return new ResearchBetScenarioResult(
        new ResearchBetParlayScenario(0m, 1m, 0m, 0m),
        new ResearchBetSinglesScenario(0m, 0m, 0m, []));
    }

    return new ResearchBetScenarioResult(CalculateParlay(active), CalculateSingles(active));
  }

  public static ResearchBetScenarioResult Calculate(IEnumerable<BetSelection> selections) =>
    Calculate(selections
      .Select(s => new ResearchBetScenarioLegInput(s.OddsAtPlacement, s.BetStatus))
      .ToList());

  private static ResearchBetParlayScenario CalculateParlay(IReadOnlyList<ResearchBetScenarioLegInput> legs)
  {
    var n = legs.Count;
    var stake = n * UnitStake;
    var combinedOdds = legs.Aggregate(1m, (acc, l) => acc * l.Odds);
    var potentialPayout = stake * combinedOdds;
    var outcome = RollupOutcome(legs);

    decimal? profit = outcome switch
    {
      BetStatus.Won => potentialPayout - stake,
      BetStatus.Lost => -stake,
      BetStatus.Canceled => 0m,
      _ => null,
    };

    return new ResearchBetParlayScenario(stake, combinedOdds, potentialPayout, profit);
  }

  private static ResearchBetSinglesScenario CalculateSingles(IReadOnlyList<ResearchBetScenarioLegInput> legs)
  {
    var breakdown = legs
      .Select(l =>
      {
        var stake = UnitStake;
        decimal? profit = l.Status switch
        {
          BetStatus.Won => stake * l.Odds - stake,
          BetStatus.Lost => -stake,
          BetStatus.Canceled => 0m,
          _ => null,
        };
        return new ResearchBetSingleLegResult(stake, l.Odds, l.Status, profit);
      })
      .ToList();

    var stakeTotal = breakdown.Sum(b => b.Stake);
    var potentialPayout = breakdown.Sum(b => b.Stake * b.Odds);
    var anyPending = breakdown.Any(b => b.Status == BetStatus.Pending);
    decimal? profit = anyPending
      ? null
      : breakdown.Sum(b => b.Profit ?? 0m);

    return new ResearchBetSinglesScenario(stakeTotal, potentialPayout, profit, breakdown);
  }

  private static BetStatus RollupOutcome(IReadOnlyList<ResearchBetScenarioLegInput> legs)
  {
    if (legs.Any(l => l.Status == BetStatus.Lost))
    {
      return BetStatus.Lost;
    }

    if (legs.All(l => l.Status == BetStatus.Won))
    {
      return BetStatus.Won;
    }

    if (legs.All(l => l.Status == BetStatus.Canceled))
    {
      return BetStatus.Canceled;
    }

    return BetStatus.Pending;
  }
}

public readonly record struct ResearchBetScenarioLegInput(decimal Odds, BetStatus Status);

public record ResearchBetScenarioResult(
  ResearchBetParlayScenario Parlay,
  ResearchBetSinglesScenario Singles);

public record ResearchBetParlayScenario(
  decimal StakeTotal,
  decimal CombinedOdds,
  decimal PotentialPayout,
  decimal? Profit);

public record ResearchBetSinglesScenario(
  decimal StakeTotal,
  decimal PotentialPayout,
  decimal? Profit,
  IReadOnlyList<ResearchBetSingleLegResult> Legs);

public record ResearchBetSingleLegResult(
  decimal Stake,
  decimal Odds,
  BetStatus Status,
  decimal? Profit);
