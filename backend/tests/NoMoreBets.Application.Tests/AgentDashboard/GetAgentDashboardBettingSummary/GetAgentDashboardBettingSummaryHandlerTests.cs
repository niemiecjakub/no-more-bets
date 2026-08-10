using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.AgentDashboard.GetAgentDashboardBettingSummary;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;

namespace NoMoreBets.Application.Tests.AgentDashboard.GetAgentDashboardBettingSummary;

public class GetAgentDashboardBettingSummaryHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _bettingRepository = Substitute.For<IBettingRepository>();
  private readonly GetAgentDashboardBettingSummaryHandler _sut;

  public GetAgentDashboardBettingSummaryHandlerTests()
  {
    _unitOfWork.Betting.Returns(_bettingRepository);
    _sut = new GetAgentDashboardBettingSummaryHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_ForwardsSeasonYearsToRepository()
  {
    // Arrange
    var seasonYears = new[] { "2025/2026", "2024/2025" };
    _bettingRepository
      .GetBettingPhaseSettledSummaryAsync(
        Arg.Any<IReadOnlyList<string>?>(),
        Arg.Any<CancellationToken>())
      .Returns(new BettingPhaseSummaryStats(4, 10, 3, 1));

    // Act
    var result = await _sut.Handle(
      new GetAgentDashboardBettingSummaryQuery(seasonYears),
      CancellationToken.None);

    // Assert
    result.SettledSlipsCount.Should().Be(4);
    result.WonSlipsCount.Should().Be(3);
    await _bettingRepository.Received(1).GetBettingPhaseSettledSummaryAsync(
      Arg.Is<IReadOnlyList<string>?>(years => years != null && years.SequenceEqual(seasonYears)),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ForwardsEmptySeasonYears_WhenUnfiltered()
  {
    // Arrange
    _bettingRepository
      .GetBettingPhaseSettledSummaryAsync(
        Arg.Any<IReadOnlyList<string>?>(),
        Arg.Any<CancellationToken>())
      .Returns(new BettingPhaseSummaryStats(0, 0, 0, 0));

    // Act
    await _sut.Handle(new GetAgentDashboardBettingSummaryQuery([]), CancellationToken.None);

    // Assert
    await _bettingRepository.Received(1).GetBettingPhaseSettledSummaryAsync(
      Arg.Is<IReadOnlyList<string>?>(years => years != null && years.Count == 0),
      Arg.Any<CancellationToken>());
  }
}
