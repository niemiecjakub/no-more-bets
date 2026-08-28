using FluentAssertions;
using NSubstitute;
using MediatR;
using NoMoreBets.Application.Betting.DailySlip;
using NoMoreBets.Application.Betting.GetMatchesAvailableForDailySlip;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Betting.DailySlip;

public class DailySlipScheduleGateTests
{
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();
  private readonly IAgentSessionRepository _sessions = Substitute.For<IAgentSessionRepository>();
  private readonly DailySlipScheduleGate _sut;
  private static readonly DateTime UtcNow = new(2026, 8, 28, 6, 0, 0, DateTimeKind.Utc);

  public DailySlipScheduleGateTests()
  {
    _unitOfWork.Betting.Returns(_betting);
    _unitOfWork.AgentSessions.Returns(_sessions);
    _sut = new DailySlipScheduleGate(_mediator, _unitOfWork);
  }

  [Fact]
  public async Task GetSkipReasonAsync_WhenNoMatches_ReturnsSkip()
  {
    // Arrange
    _mediator.Send(Arg.Any<GetMatchesAvailableForDailySlipQuery>(), Arg.Any<CancellationToken>())
      .Returns(Array.Empty<Match>());

    // Act
    var reason = await _sut.GetSkipReasonAsync(UtcNow, CancellationToken.None);

    // Assert
    reason.Should().Contain("no matches");
  }

  [Fact]
  public async Task GetSkipReasonAsync_WhenDailyPickExists_ReturnsSkip()
  {
    // Arrange
    _mediator.Send(Arg.Any<GetMatchesAvailableForDailySlipQuery>(), Arg.Any<CancellationToken>())
      .Returns(new List<Match> { new() { Id = 1 } });
    _betting.AnyDailyPickOnDateAsync(new DateOnly(2026, 8, 28), Arg.Any<CancellationToken>())
      .Returns(true);

    // Act
    var reason = await _sut.GetSkipReasonAsync(UtcNow, CancellationToken.None);

    // Assert
    reason.Should().Contain("daily pick");
  }

  [Fact]
  public async Task GetSkipReasonAsync_WhenSessionExists_ReturnsSkip()
  {
    // Arrange
    _mediator.Send(Arg.Any<GetMatchesAvailableForDailySlipQuery>(), Arg.Any<CancellationToken>())
      .Returns(new List<Match> { new() { Id = 1 } });
    _betting.AnyDailyPickOnDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
      .Returns(false);
    _sessions.AnySessionInRangeAsync(
        AgentSessionPhase.DailySlip,
        Arg.Any<DateTime>(),
        Arg.Any<DateTime>(),
        Arg.Any<CancellationToken>())
      .Returns(true);

    // Act
    var reason = await _sut.GetSkipReasonAsync(UtcNow, CancellationToken.None);

    // Assert
    reason.Should().Contain("session");
  }

  [Fact]
  public async Task GetSkipReasonAsync_WhenCardIsClear_ReturnsNull()
  {
    // Arrange
    _mediator.Send(Arg.Any<GetMatchesAvailableForDailySlipQuery>(), Arg.Any<CancellationToken>())
      .Returns(new List<Match> { new() { Id = 1 } });
    _betting.AnyDailyPickOnDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
      .Returns(false);
    _sessions.AnySessionInRangeAsync(
        AgentSessionPhase.DailySlip,
        Arg.Any<DateTime>(),
        Arg.Any<DateTime>(),
        Arg.Any<CancellationToken>())
      .Returns(false);

    // Act
    var reason = await _sut.GetSkipReasonAsync(UtcNow, CancellationToken.None);

    // Assert
    reason.Should().BeNull();
  }
}
