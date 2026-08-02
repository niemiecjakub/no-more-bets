using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Betting.GetMatchResearchBetSlip;
using NoMoreBets.Application.Betting.ResearchBetScenarioStats;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Tests.Betting.ResearchBetScenarioStats;

public class GetMatchResearchBetSlipWithScenariosHandlerTests
{
  private readonly ISender _sender = Substitute.For<ISender>();
  private readonly IResearchBetScenarioStatsService _scenarioStats = Substitute.For<IResearchBetScenarioStatsService>();
  private readonly GetMatchResearchBetSlipWithScenariosHandler _sut;

  public GetMatchResearchBetSlipWithScenariosHandlerTests()
  {
    _sut = new GetMatchResearchBetSlipWithScenariosHandler(_sender, _scenarioStats);
  }

  [Fact]
  public async Task Handle_WhenNoSlip_ReturnsNull()
  {
    // Arrange
    _sender.Send(Arg.Any<GetMatchResearchBetSlipQuery>(), Arg.Any<CancellationToken>())
      .Returns((BetSlipSummary?)null);

    // Act
    var result = await _sut.Handle(new GetMatchResearchBetSlipWithScenariosQuery(1), CancellationToken.None);

    // Assert
    result.Should().BeNull();
    _scenarioStats.DidNotReceive().FromSummary(Arg.Any<BetSlipSummary>());
  }

  [Fact]
  public async Task Handle_WhenSlipExists_ReturnsSummaryAndScenarios()
  {
    // Arrange
    var slip = new BetSlipSummary(
      10,
      new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc),
      10m,
      2m,
      20m,
      BetStatus.Won,
      [
        new BetSelectionSummary(5, "A", "B", "BTTS", "Yes", 2m, BetStatus.Won)
      ],
      null,
      null);
    var scenarios = new ResearchBetScenarioStatsDto(
      ResearchBetScenarioCalculator.UnitStake,
      new ResearchBetParlayScenarioDto(5m, 2m, 10m, 5m),
      new ResearchBetSinglesScenarioDto(5m, 10m, 5m, [
        new ResearchBetSingleLegDto(5m, 2m, BetStatus.Won, 5m)
      ]));

    _sender.Send(Arg.Any<GetMatchResearchBetSlipQuery>(), Arg.Any<CancellationToken>())
      .Returns(slip);
    _scenarioStats.FromSummary(slip).Returns(scenarios);

    // Act
    var result = await _sut.Handle(new GetMatchResearchBetSlipWithScenariosQuery(5), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.Slip.Should().BeSameAs(slip);
    result.Scenarios.Should().BeSameAs(scenarios);
  }

  [Fact]
  public async Task Handle_WhenSlipPending_ReturnsSummaryWithoutScenarios()
  {
    // Arrange
    var slip = new BetSlipSummary(
      10,
      new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc),
      10m,
      2m,
      20m,
      BetStatus.Pending,
      [
        new BetSelectionSummary(5, "A", "B", "BTTS", "Yes", 2m, BetStatus.Pending)
      ],
      null,
      null);

    _sender.Send(Arg.Any<GetMatchResearchBetSlipQuery>(), Arg.Any<CancellationToken>())
      .Returns(slip);

    // Act
    var result = await _sut.Handle(new GetMatchResearchBetSlipWithScenariosQuery(5), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.Slip.Should().BeSameAs(slip);
    result.Scenarios.Should().BeNull();
    _scenarioStats.DidNotReceive().FromSummary(Arg.Any<BetSlipSummary>());
  }
}

public class ResearchBetScenarioStatsServiceTests
{
  private readonly ResearchBetScenarioStatsService _sut = new();

  [Fact]
  public void FromSummary_SingleWonLeg_ReturnsParlayAndSinglesProfit()
  {
    // Arrange
    var slip = new BetSlipSummary(
      10,
      DateTime.UtcNow,
      10m,
      2m,
      20m,
      BetStatus.Won,
      [new BetSelectionSummary(5, "A", "B", "BTTS", "Yes", 2m, BetStatus.Won)],
      null,
      null);

    // Act
    var result = _sut.FromSummary(slip);

    // Assert
    result.UnitStake.Should().Be(ResearchBetScenarioCalculator.UnitStake);
    result.Parlay.StakeTotal.Should().Be(5m);
    result.Parlay.Profit.Should().Be(5m);
    result.Singles.Profit.Should().Be(5m);
  }
}
