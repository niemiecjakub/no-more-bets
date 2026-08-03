using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.AgentDashboard.GetAgentDashboardResearchBettingSummary;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Tests.AgentDashboard.GetAgentDashboardResearchBettingSummary;

public class GetAgentDashboardResearchBettingSummaryHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _bettingRepository = Substitute.For<IBettingRepository>();
  private readonly GetAgentDashboardResearchBettingSummaryHandler _sut;

  public GetAgentDashboardResearchBettingSummaryHandlerTests()
  {
    _unitOfWork.Betting.Returns(_bettingRepository);
    _sut = new GetAgentDashboardResearchBettingSummaryHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_EmptyLegs_ReturnsZeroScenarioAggregates()
  {
    // Arrange
    _bettingRepository
      .GetResearchPhaseSettledSummaryAsync(
        Arg.Any<IReadOnlyList<int>>(),
        Arg.Any<IReadOnlyList<string>>(),
        Arg.Any<CancellationToken>())
      .Returns(new ResearchPhaseSummaryStats(0, 0, 0));
    _bettingRepository
      .GetResearchPhaseSettledScenarioLegsAsync(
        Arg.Any<IReadOnlyList<int>>(),
        Arg.Any<IReadOnlyList<string>>(),
        Arg.Any<CancellationToken>())
      .Returns([]);

    // Act
    var result = await _sut.Handle(
      new GetAgentDashboardResearchBettingSummaryQuery([], []),
      CancellationToken.None);

    // Assert
    result.UnitStake.Should().Be(ResearchBetScenarioCalculator.UnitStake);
    result.ScenarioSlipCount.Should().Be(0);
    result.Parlay.Should().BeEquivalentTo(new ResearchScenarioPnlDto(0m, 0m, 0m));
    result.Singles.Should().BeEquivalentTo(new ResearchScenarioPnlDto(0m, 0m, 0m));
  }

  [Fact]
  public async Task Handle_TwoSettledSlips_SumsParlayAndSinglesProfit()
  {
    // Arrange
    // Slip 1: 3-leg all won @ 2.0, 1.5, 1.8
    // Slip 2: 2-leg one lost @ 2.0 won, 1.5 lost
    _bettingRepository
      .GetResearchPhaseSettledSummaryAsync(
        Arg.Any<IReadOnlyList<int>>(),
        Arg.Any<IReadOnlyList<string>>(),
        Arg.Any<CancellationToken>())
      .Returns(new ResearchPhaseSummaryStats(5, 4, 1));
    _bettingRepository
      .GetResearchPhaseSettledScenarioLegsAsync(
        Arg.Any<IReadOnlyList<int>>(),
        Arg.Any<IReadOnlyList<string>>(),
        Arg.Any<CancellationToken>())
      .Returns(
      [
        new ResearchPhaseScenarioLegRow(1, 2.0m, BetStatus.Won),
        new ResearchPhaseScenarioLegRow(1, 1.5m, BetStatus.Won),
        new ResearchPhaseScenarioLegRow(1, 1.8m, BetStatus.Won),
        new ResearchPhaseScenarioLegRow(2, 2.0m, BetStatus.Won),
        new ResearchPhaseScenarioLegRow(2, 1.5m, BetStatus.Lost),
      ]);

    // Act
    var result = await _sut.Handle(
      new GetAgentDashboardResearchBettingSummaryQuery([], []),
      CancellationToken.None);

    // Assert
    var slip1Combined = 2.0m * 1.5m * 1.8m;
    var slip1ParlayProfit = 15m * slip1Combined - 15m;
    var slip1SinglesProfit = (5m * 2.0m - 5m) + (5m * 1.5m - 5m) + (5m * 1.8m - 5m);
    var slip2ParlayProfit = -10m;
    var slip2SinglesProfit = (5m * 2.0m - 5m) + (-5m);

    result.SettledSelectionsCount.Should().Be(5);
    result.WonSelectionsCount.Should().Be(4);
    result.LostSelectionsCount.Should().Be(1);
    result.WinRatePercent.Should().Be(80m);
    result.LossRatePercent.Should().Be(20m);
    result.ScenarioSlipCount.Should().Be(2);

    result.Parlay.StakeTotal.Should().Be(25m);
    result.Parlay.Profit.Should().Be(slip1ParlayProfit + slip2ParlayProfit);
    result.Parlay.Roi.Should().Be(Math.Round((slip1ParlayProfit + slip2ParlayProfit) / 25m, 4));

    result.Singles.StakeTotal.Should().Be(25m);
    result.Singles.Profit.Should().Be(slip1SinglesProfit + slip2SinglesProfit);
    result.Singles.Roi.Should().Be(Math.Round((slip1SinglesProfit + slip2SinglesProfit) / 25m, 4));
  }

  [Fact]
  public async Task Handle_SkipsSlipWithPendingProfit()
  {
    // Arrange
    _bettingRepository
      .GetResearchPhaseSettledSummaryAsync(
        Arg.Any<IReadOnlyList<int>>(),
        Arg.Any<IReadOnlyList<string>>(),
        Arg.Any<CancellationToken>())
      .Returns(new ResearchPhaseSummaryStats(3, 1, 1));
    _bettingRepository
      .GetResearchPhaseSettledScenarioLegsAsync(
        Arg.Any<IReadOnlyList<int>>(),
        Arg.Any<IReadOnlyList<string>>(),
        Arg.Any<CancellationToken>())
      .Returns(
      [
        new ResearchPhaseScenarioLegRow(1, 2.0m, BetStatus.Won),
        new ResearchPhaseScenarioLegRow(1, 1.5m, BetStatus.Pending),
        new ResearchPhaseScenarioLegRow(2, 2.0m, BetStatus.Lost),
      ]);

    // Act
    var result = await _sut.Handle(
      new GetAgentDashboardResearchBettingSummaryQuery([], []),
      CancellationToken.None);

    // Assert
    result.ScenarioSlipCount.Should().Be(1);
    result.Parlay.StakeTotal.Should().Be(5m);
    result.Parlay.Profit.Should().Be(-5m);
    result.Singles.StakeTotal.Should().Be(5m);
    result.Singles.Profit.Should().Be(-5m);
  }

  [Fact]
  public async Task Handle_PassesSeasonYearsToRepository()
  {
    // Arrange
    var seasonYears = new[] { "2025-2026", "2026-2027" };
    _bettingRepository
      .GetResearchPhaseSettledSummaryAsync(
        Arg.Any<IReadOnlyList<int>>(),
        Arg.Any<IReadOnlyList<string>>(),
        Arg.Any<CancellationToken>())
      .Returns(new ResearchPhaseSummaryStats(0, 0, 0));
    _bettingRepository
      .GetResearchPhaseSettledScenarioLegsAsync(
        Arg.Any<IReadOnlyList<int>>(),
        Arg.Any<IReadOnlyList<string>>(),
        Arg.Any<CancellationToken>())
      .Returns([]);

    // Act
    await _sut.Handle(
      new GetAgentDashboardResearchBettingSummaryQuery([], seasonYears),
      CancellationToken.None);

    // Assert
    await _bettingRepository.Received(1).GetResearchPhaseSettledSummaryAsync(
      Arg.Any<IReadOnlyList<int>>(),
      Arg.Is<IReadOnlyList<string>>(years => years.SequenceEqual(seasonYears)),
      Arg.Any<CancellationToken>());
    await _bettingRepository.Received(1).GetResearchPhaseSettledScenarioLegsAsync(
      Arg.Any<IReadOnlyList<int>>(),
      Arg.Is<IReadOnlyList<string>>(years => years.SequenceEqual(seasonYears)),
      Arg.Any<CancellationToken>());
  }
}
