using FluentAssertions;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Tests.Betting;

public class ResearchBetScenarioCalculatorTests
{
  [Fact]
  public void Calculate_ThreeLegsAllWon_ParlayAndSinglesProfitMatchFormula()
  {
    // Arrange
    var legs = new[]
    {
      new ResearchBetScenarioLegInput(2.0m, BetStatus.Won),
      new ResearchBetScenarioLegInput(1.5m, BetStatus.Won),
      new ResearchBetScenarioLegInput(1.8m, BetStatus.Won),
    };

    // Act
    var result = ResearchBetScenarioCalculator.Calculate(legs);

    // Assert
    var combinedOdds = 2.0m * 1.5m * 1.8m;
    result.Parlay.StakeTotal.Should().Be(15m);
    result.Parlay.CombinedOdds.Should().Be(combinedOdds);
    result.Parlay.Profit.Should().Be(15m * combinedOdds - 15m);

    result.Singles.StakeTotal.Should().Be(15m);
    result.Singles.Profit.Should().Be(
      (5m * 2.0m - 5m) + (5m * 1.5m - 5m) + (5m * 1.8m - 5m));
    result.Singles.Legs.Should().HaveCount(3);
  }

  [Fact]
  public void Calculate_AnyLost_ParlayLosesFullStake_SinglesMixesPnL()
  {
    // Arrange
    var legs = new[]
    {
      new ResearchBetScenarioLegInput(2.0m, BetStatus.Won),
      new ResearchBetScenarioLegInput(1.5m, BetStatus.Lost),
      new ResearchBetScenarioLegInput(1.8m, BetStatus.Won),
    };

    // Act
    var result = ResearchBetScenarioCalculator.Calculate(legs);

    // Assert
    result.Parlay.StakeTotal.Should().Be(15m);
    result.Parlay.Profit.Should().Be(-15m);

    result.Singles.StakeTotal.Should().Be(15m);
    result.Singles.Profit.Should().Be(
      (5m * 2.0m - 5m) + (-5m) + (5m * 1.8m - 5m));
  }

  [Fact]
  public void Calculate_PendingLeg_ProfitIsNull()
  {
    // Arrange
    var legs = new[]
    {
      new ResearchBetScenarioLegInput(2.0m, BetStatus.Won),
      new ResearchBetScenarioLegInput(1.5m, BetStatus.Pending),
    };

    // Act
    var result = ResearchBetScenarioCalculator.Calculate(legs);

    // Assert
    result.Parlay.Profit.Should().BeNull();
    result.Singles.Profit.Should().BeNull();
    result.Singles.Legs[1].Profit.Should().BeNull();
  }

  [Fact]
  public void Calculate_IgnoresCanceledLegs()
  {
    // Arrange
    var legs = new[]
    {
      new ResearchBetScenarioLegInput(2.0m, BetStatus.Won),
      new ResearchBetScenarioLegInput(3.0m, BetStatus.Canceled),
    };

    // Act
    var result = ResearchBetScenarioCalculator.Calculate(legs);

    // Assert
    result.Parlay.StakeTotal.Should().Be(5m);
    result.Parlay.CombinedOdds.Should().Be(2.0m);
    result.Parlay.Profit.Should().Be(5m);
    result.Singles.Legs.Should().ContainSingle();
  }
}
