using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.AgentDashboard.GetAgentDashboardBettingSummaryDetails;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;

namespace NoMoreBets.Application.Tests.AgentDashboard.GetAgentDashboardBettingSummaryDetails;

public class GetAgentDashboardBettingSummaryDetailsHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _bettingRepository = Substitute.For<IBettingRepository>();
  private readonly GetAgentDashboardBettingSummaryDetailsHandler _sut;

  public GetAgentDashboardBettingSummaryDetailsHandlerTests()
  {
    _unitOfWork.Betting.Returns(_bettingRepository);
    _sut = new GetAgentDashboardBettingSummaryDetailsHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_ForwardsSeasonYearsToRepository()
  {
    // Arrange
    var seasonYears = new[] { "2025/2026" };
    _bettingRepository
      .GetBettingPhaseSettledDetailCountsAsync(
        Arg.Any<IReadOnlyList<string>?>(),
        Arg.Any<CancellationToken>())
      .Returns(new BettingPhaseDetailCounts(2, 1, 5, 3));

    // Act
    var result = await _sut.Handle(
      new GetAgentDashboardBettingSummaryDetailsQuery(seasonYears),
      CancellationToken.None);

    // Assert
    result.WonSlipsCount.Should().Be(2);
    result.LostSelectionsCount.Should().Be(3);
    await _bettingRepository.Received(1).GetBettingPhaseSettledDetailCountsAsync(
      Arg.Is<IReadOnlyList<string>?>(years => years != null && years.SequenceEqual(seasonYears)),
      Arg.Any<CancellationToken>());
  }
}
