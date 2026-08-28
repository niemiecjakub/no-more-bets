using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.Tests.AI.Plugins;

public class DailySlipToolTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();
  private readonly IBankrollRepository _bankroll = Substitute.For<IBankrollRepository>();
  private readonly AgentSessionContext _agentSessionContext = new();
  private readonly DailySlipTool _sut;

  public DailySlipToolTests()
  {
    var mediator = Substitute.For<IMediator>();
    _unitOfWork.Betting.Returns(_betting);
    _unitOfWork.Bankroll.Returns(_bankroll);
    _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    _agentSessionContext.SessionId = 42;
    _sut = new DailySlipTool(_unitOfWork, mediator, _agentSessionContext);
  }

  [Fact]
  public async Task PlaceBetSlip_WhenValid_AddsPaperSlipWithDailyPick()
  {
    // Arrange
    const string json =
      """{"betSelections":[{"matchId":39,"eventType":"bothTeamsToScore","eventOption":"bothTeamsToScore_Yes"}]}""";
    _betting
      .GetCurrentOddsForSelectionAsync(39, BettingEventType.BothTeamsToScore, BettingEventOption.BothTeamsToScore_Yes, Arg.Any<CancellationToken>())
      .Returns(2.0m);
    _betting
      .AnyDailyPickOnDateWithRiskAsync(Arg.Any<DateOnly>(), (int)BetRiskLevel.Low, Arg.Any<CancellationToken>())
      .Returns(false);

    // Act
    var result = await _sut.PlaceBetSlip(
      BetRiskLevel.Low,
      json,
      "Home side covers.",
      0.55m,
      CancellationToken.None);

    // Assert
    result.Should().Be("Daily slip placed successfully.");
    await _betting.Received(1).AddBetSlipAsync(
      Arg.Is<BetSlip>(s =>
        s.StakeAmount == 10m
        && s.AgentSessionId == 42
        && s.Bankrolls.Count == 0
        && s.DailyPick != null
        && s.DailyPick.RiskLevelId == (int)BetRiskLevel.Low
        && s.DailyPick.SlipDate == WarsawCalendar.DateFromUtc(DateTime.UtcNow)
        && s.Selections.Count == 1
        && s.Selections.Single().MatchId == 39),
      Arg.Any<CancellationToken>());
    await _bankroll.DidNotReceive().AddAsync(Arg.Any<Bankroll>(), Arg.Any<CancellationToken>());
    await _bankroll.DidNotReceive().GetCurrentBalanceAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task PlaceBetSlip_WhenRiskAlreadyPlacedToday_ReturnsErrorWithoutSaving()
  {
    // Arrange
    const string json =
      """{"betSelections":[{"matchId":39,"eventType":"bothTeamsToScore","eventOption":"bothTeamsToScore_Yes"}]}""";
    _betting
      .AnyDailyPickOnDateWithRiskAsync(Arg.Any<DateOnly>(), (int)BetRiskLevel.Medium, Arg.Any<CancellationToken>())
      .Returns(true);

    // Act
    var result = await _sut.PlaceBetSlip(
      BetRiskLevel.Medium,
      json,
      "Edge.",
      0.4m,
      CancellationToken.None);

    // Assert
    result.Should().Contain("Medium");
    await _betting.DidNotReceive().AddBetSlipAsync(Arg.Any<BetSlip>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task PlaceBetSlip_WhenOddsMissing_ReturnsErrorWithoutSaving()
  {
    // Arrange
    const string json =
      """{"betSelections":[{"matchId":39,"eventType":"bothTeamsToScore","eventOption":"bothTeamsToScore_Yes"}]}""";
    _betting
      .AnyDailyPickOnDateWithRiskAsync(Arg.Any<DateOnly>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
      .Returns(false);
    _betting
      .GetCurrentOddsForSelectionAsync(39, BettingEventType.BothTeamsToScore, BettingEventOption.BothTeamsToScore_Yes, Arg.Any<CancellationToken>())
      .Returns((decimal?)null);

    // Act
    var result = await _sut.PlaceBetSlip(
      BetRiskLevel.High,
      json,
      "Edge.",
      0.3m,
      CancellationToken.None);

    // Assert
    result.Should().Contain("Current odds not found");
    await _betting.DidNotReceive().AddBetSlipAsync(Arg.Any<BetSlip>(), Arg.Any<CancellationToken>());
  }
}
