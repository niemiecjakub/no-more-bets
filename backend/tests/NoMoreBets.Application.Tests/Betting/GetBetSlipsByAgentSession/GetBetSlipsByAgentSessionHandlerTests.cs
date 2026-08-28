using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Betting.GetBetSlipsByAgentSession;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Betting;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Betting.GetBetSlipsByAgentSession;

public class GetBetSlipsByAgentSessionHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();
  private readonly IAgentSessionRepository _sessions = Substitute.For<IAgentSessionRepository>();
  private readonly GetBetSlipsByAgentSessionHandler _sut;

  public GetBetSlipsByAgentSessionHandlerTests()
  {
    _unitOfWork.Betting.Returns(_betting);
    _unitOfWork.AgentSessions.Returns(_sessions);
    _sut = new GetBetSlipsByAgentSessionHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenSessionMissing_ReturnsNull()
  {
    // Arrange
    _sessions.SessionExistsAsync(9, Arg.Any<CancellationToken>()).Returns(false);

    // Act
    var result = await _sut.Handle(new GetBetSlipsByAgentSessionQuery(9), CancellationToken.None);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task Handle_WhenSessionExists_MapsSlipsIncludingDailyPick()
  {
    // Arrange
    _sessions.SessionExistsAsync(42, Arg.Any<CancellationToken>()).Returns(true);
    var match = new Match
    {
      Id = 9,
      HomeClub = new ClubEntity { Name = "Arsenal", Slug = "arsenal" },
      AwayClub = new ClubEntity { Name = "Chelsea", Slug = "chelsea" }
    };
    var slip = new BetSlip
    {
      Id = 1,
      AgentSessionId = 42,
      CreatedAt = new DateTime(2026, 8, 28, 6, 0, 0, DateTimeKind.Utc),
      StakeAmount = 10m,
      TotalOdds = 2m,
      PotentialPayout = 20m,
      BetStatus = BetStatus.Pending,
      BetStatusEntity = new BetStatusEntity { Id = 1, Name = "Pending" },
      DailyPick = new DailyPick
      {
        BetSlipId = 1,
        RiskLevelId = (int)BetRiskLevel.Low,
        SlipDate = new DateOnly(2026, 8, 28),
        RiskLevel = new BetRiskLevelEntity { Id = 1, Name = "Low" }
      },
      Selections =
      [
        new BetSelection
        {
          Id = 1,
          MatchId = 9,
          BetEventType = BettingEventType.MatchResult,
          BetEventOption = BettingEventOption.MatchResult_Home,
          OddsAtPlacement = 2m,
          BetStatus = BetStatus.Pending,
          BetStatusEntity = new BetStatusEntity { Id = 1, Name = "Pending" },
          Match = match
        }
      ]
    };
    _betting.GetBetSlipsByAgentSessionIdAsync(42, Arg.Any<CancellationToken>())
      .Returns(new List<BetSlip> { slip });

    // Act
    var result = await _sut.Handle(new GetBetSlipsByAgentSessionQuery(42), CancellationToken.None);

    // Assert
    result.Should().ContainSingle();
    result![0].RiskLevelName.Should().Be("Low");
    result[0].AgentSessionId.Should().Be(42);
  }
}
