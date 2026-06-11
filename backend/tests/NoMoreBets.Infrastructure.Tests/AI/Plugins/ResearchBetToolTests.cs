using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.Tests.AI.Plugins;

public class ResearchBetToolTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();
  private readonly AgentSessionContext _agentSessionContext = new();
  private readonly ResearchBetTool _sut;

  public ResearchBetToolTests()
  {
    _unitOfWork.Betting.Returns(_betting);
    _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    _agentSessionContext.SessionId = 42;
    _sut = new ResearchBetTool(7, _unitOfWork, _agentSessionContext);
  }

  [Theory]
  [InlineData("""{"betSelections":[{"eventType":"BothTeamsToScore","eventOption":"BothTeamsToScore_Yes"}]}""")]
  [InlineData("""[{"eventType":"BothTeamsToScore","eventOption":"BothTeamsToScore_Yes"}]""")]
  [InlineData("""{"betSelections":[{"eventType":"bothTeamsToScore","option":"bothTeamsToScore_Yes"}]}""")]
  public async Task PlaceBetSlip_WhenValidJson_AddsSlip(string json)
  {
    // Arrange
    _betting
      .GetCurrentOddsForSelectionAsync(7, BettingEventType.BothTeamsToScore, BettingEventOption.BothTeamsToScore_Yes, Arg.Any<CancellationToken>())
      .Returns(2.0m);

    // Act
    var result = await _sut.PlaceBetSlip(json, CancellationToken.None);

    // Assert
    result.Should().Be("Research bet slip placed successfully.");
    await _betting.Received(1).AddBetSlipAsync(
      Arg.Is<BetSlip>(s =>
        s.StakeAmount == 10m
        && s.TotalOdds == 2.0m
        && s.Selections.Count == 1
        && s.Selections.Single().MatchId == 7),
      Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task PlaceBetSlip_WhenInvalidJson_ReturnsErrorWithoutThrowing()
  {
    // Act
    var result = await _sut.PlaceBetSlip("not-json", CancellationToken.None);

    // Assert
    result.Should().Contain("Invalid betSelections JSON");
    await _betting.DidNotReceive().AddBetSlipAsync(Arg.Any<BetSlip>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task PlaceBetSlip_WhenOddsMissing_ReturnsErrorWithoutThrowing()
  {
    // Arrange
    const string json =
      """{"betSelections":[{"eventType":"BothTeamsToScore","eventOption":"BothTeamsToScore_Yes"}]}""";
    _betting
      .GetCurrentOddsForSelectionAsync(7, BettingEventType.BothTeamsToScore, BettingEventOption.BothTeamsToScore_Yes, Arg.Any<CancellationToken>())
      .Returns((decimal?)null);

    // Act
    var result = await _sut.PlaceBetSlip(json, CancellationToken.None);

    // Assert
    result.Should().Contain("Current odds not found");
    await _betting.DidNotReceive().AddBetSlipAsync(Arg.Any<BetSlip>(), Arg.Any<CancellationToken>());
  }
}
