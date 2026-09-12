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
  private readonly GetMatchResearchBetSlipWithScenariosHandler _sut;

  public GetMatchResearchBetSlipWithScenariosHandlerTests()
  {
    _sut = new GetMatchResearchBetSlipWithScenariosHandler(_sender);
  }

  [Fact]
  public async Task Handle_WhenNoSlip_ReturnsNull()
  {
    _sender.Send(Arg.Any<GetMatchResearchBetSlipQuery>(), Arg.Any<CancellationToken>())
      .Returns((BetSlipSummary?)null);

    var result = await _sut.Handle(new GetMatchResearchBetSlipWithScenariosQuery(1), CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task Handle_WhenSlipExists_ReturnsSummaryAndScenarios()
  {
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

    _sender.Send(Arg.Any<GetMatchResearchBetSlipQuery>(), Arg.Any<CancellationToken>())
      .Returns(slip);

    var result = await _sut.Handle(new GetMatchResearchBetSlipWithScenariosQuery(5), CancellationToken.None);

    result.Should().NotBeNull();
    result!.Slip.Should().BeSameAs(slip);
    result.Scenarios.Should().NotBeNull();
    result.Scenarios!.UnitStake.Should().Be(ResearchBetScenarioCalculator.UnitStake);
    result.Scenarios.Parlay.Profit.Should().Be(5m);
  }

  [Fact]
  public async Task Handle_WhenSlipPending_ReturnsSummaryWithoutScenarios()
  {
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

    var result = await _sut.Handle(new GetMatchResearchBetSlipWithScenariosQuery(5), CancellationToken.None);

    result.Should().NotBeNull();
    result!.Slip.Should().BeSameAs(slip);
    result.Scenarios.Should().BeNull();
  }

  [Fact]
  public void FromSummary_SingleWonLeg_ReturnsParlayAndSinglesProfit()
  {
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

    var result = GetMatchResearchBetSlipWithScenariosHandler.FromSummary(slip);

    result.UnitStake.Should().Be(ResearchBetScenarioCalculator.UnitStake);
    result.Parlay.StakeTotal.Should().Be(5m);
    result.Parlay.Profit.Should().Be(5m);
    result.Singles.Profit.Should().Be(5m);
  }
}
