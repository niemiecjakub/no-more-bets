using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.AgentDashboard.GetAgentDashboardPendingBets;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;

namespace NoMoreBets.Application.Tests.AgentDashboard.GetAgentDashboardPendingBets;

public class GetAgentDashboardPendingBetsHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _bettingRepository = Substitute.For<IBettingRepository>();
  private readonly GetAgentDashboardPendingBetsHandler _sut;

  public GetAgentDashboardPendingBetsHandlerTests()
  {
    _unitOfWork.Betting.Returns(_bettingRepository);
    _sut = new GetAgentDashboardPendingBetsHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_ForwardsSeasonYearsToRepository()
  {
    // Arrange
    var seasonYears = new[] { "2025/2026" };
    var latest = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
    _bettingRepository
      .GetBettingPhasePendingBetsWidgetAsync(
        Arg.Any<IReadOnlyList<string>?>(),
        Arg.Any<CancellationToken>())
      .Returns(new PendingBetsWidgetData(2, 100m, 250m, latest));

    // Act
    var result = await _sut.Handle(
      new GetAgentDashboardPendingBetsQuery(seasonYears),
      CancellationToken.None);

    // Assert
    result.PendingSlipsCount.Should().Be(2);
    result.PendingStakeTotal.Should().Be(100m);
    await _bettingRepository.Received(1).GetBettingPhasePendingBetsWidgetAsync(
      Arg.Is<IReadOnlyList<string>?>(years => years != null && years.SequenceEqual(seasonYears)),
      Arg.Any<CancellationToken>());
  }
}
