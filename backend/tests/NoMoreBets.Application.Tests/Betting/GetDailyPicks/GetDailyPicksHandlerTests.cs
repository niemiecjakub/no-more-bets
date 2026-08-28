using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Betting.GetDailyPicks;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Betting.GetDailyPicks;

public class GetDailyPicksHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();
  private readonly GetDailyPicksHandler _sut;
  private static readonly DateOnly SlipDate = new(2026, 8, 28);

  public GetDailyPicksHandlerTests()
  {
    _unitOfWork.Betting.Returns(_betting);
    _sut = new GetDailyPicksHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenNone_ReturnsEmpty()
  {
    // Arrange
    _betting.GetBetSlipsWithDailyPickOnDateAsync(SlipDate, Arg.Any<CancellationToken>())
      .Returns(new List<BetSlip>());

    // Act
    var result = await _sut.Handle(new GetDailyPicksQuery(SlipDate), CancellationToken.None);

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public async Task Handle_WhenPicksExist_MapsRiskAndSelections()
  {
    // Arrange
    var match = new Match
    {
      Id = 9,
      HomeClub = new ClubEntity { Name = "Arsenal", Slug = "arsenal" },
      AwayClub = new ClubEntity { Name = "Chelsea", Slug = "chelsea" }
    };
    var slip = new BetSlip
    {
      Id = 1,
      AgentSessionId = 7,
      CreatedAt = new DateTime(2026, 8, 28, 6, 0, 0, DateTimeKind.Utc),
      StakeAmount = 10m,
      TotalOdds = 2m,
      PotentialPayout = 20m,
      BetStatus = BetStatus.Pending,
      BetStatusEntity = new BetStatusEntity { Id = 1, Name = "Pending" },
      DailyPick = new DailyPick
      {
        BetSlipId = 1,
        RiskLevelId = (int)BetRiskLevel.Medium,
        SlipDate = SlipDate,
        RiskLevel = new BetRiskLevelEntity { Id = 2, Name = "Medium" }
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
    _betting.GetBetSlipsWithDailyPickOnDateAsync(SlipDate, Arg.Any<CancellationToken>())
      .Returns(new List<BetSlip> { slip });

    // Act
    var result = await _sut.Handle(new GetDailyPicksQuery(SlipDate), CancellationToken.None);

    // Assert
    result.Should().ContainSingle();
    result[0].RiskLevelId.Should().Be((int)BetRiskLevel.Medium);
    result[0].RiskLevelName.Should().Be("Medium");
    result[0].SlipDate.Should().Be(SlipDate);
  }
}
