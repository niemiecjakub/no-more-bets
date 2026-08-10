using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.AgentDashboard.GetAgentDashboardSessions;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;

namespace NoMoreBets.Application.Tests.AgentDashboard.GetAgentDashboardSessions;

public class GetAgentDashboardSessionsHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IAgentSessionRepository _sessionsRepository = Substitute.For<IAgentSessionRepository>();
  private readonly GetAgentDashboardSessionsHandler _sut;

  public GetAgentDashboardSessionsHandlerTests()
  {
    _unitOfWork.AgentSessions.Returns(_sessionsRepository);
    _sut = new GetAgentDashboardSessionsHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_ForwardsSeasonYearsToRepository()
  {
    // Arrange
    var seasonYears = new[] { "2025/2026" };
    var latest = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
    _sessionsRepository
      .GetSessionsWidgetAsync(
        Arg.Any<IReadOnlyList<string>?>(),
        Arg.Any<CancellationToken>())
      .Returns(new AgentSessionsWidgetData(3, latest, AgentSessionPhase.Betting.ToString()));

    // Act
    var result = await _sut.Handle(
      new GetAgentDashboardSessionsQuery(seasonYears),
      CancellationToken.None);

    // Assert
    result.SessionsCount.Should().Be(3);
    await _sessionsRepository.Received(1).GetSessionsWidgetAsync(
      Arg.Is<IReadOnlyList<string>?>(years => years != null && years.SequenceEqual(seasonYears)),
      Arg.Any<CancellationToken>());
  }
}
