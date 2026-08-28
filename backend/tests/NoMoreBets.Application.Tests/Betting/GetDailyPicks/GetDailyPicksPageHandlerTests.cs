using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Betting.GetDailyPicks;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Betting.GetDailyPicks;

public class GetDailyPicksPageHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();
  private readonly GetDailyPicksPageHandler _sut;

  public GetDailyPicksPageHandlerTests()
  {
    _unitOfWork.Betting.Returns(_betting);
    _sut = new GetDailyPicksPageHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenNone_ReturnsEmptyPage()
  {
    // Arrange
    _betting.GetDailyPickSlipsPageAsync(7, null, Arg.Any<CancellationToken>())
      .Returns(new DailyPickSlipPage([], false));

    // Act
    var result = await _sut.Handle(new GetDailyPicksPageQuery(7, null), CancellationToken.None);

    // Assert
    result.Items.Should().BeEmpty();
    result.HasMore.Should().BeFalse();
    result.NextCursorAt.Should().BeNull();
  }

  [Fact]
  public async Task Handle_WhenHasMore_SetsCursorFromOldestSlipDate()
  {
    // Arrange
    var olderDate = new DateOnly(2026, 8, 20);
    var slip = Slip(id: 3, risk: BetRiskLevel.High, slipDate: olderDate);
    _betting.GetDailyPickSlipsPageAsync(7, null, Arg.Any<CancellationToken>())
      .Returns(new DailyPickSlipPage([slip], true));

    // Act
    var result = await _sut.Handle(new GetDailyPicksPageQuery(7, null), CancellationToken.None);

    // Assert
    result.HasMore.Should().BeTrue();
    result.NextCursorAt.Should().Be(new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc));
    result.Items.Should().ContainSingle().Which.RiskLevelName.Should().Be("High");
  }

  private static BetSlip Slip(int id, BetRiskLevel risk, DateOnly slipDate)
  {
    var match = new Match
    {
      Id = 9,
      HomeClub = new ClubEntity { Name = "Arsenal", Slug = "arsenal" },
      AwayClub = new ClubEntity { Name = "Chelsea", Slug = "chelsea" }
    };
    return new BetSlip
    {
      Id = id,
      AgentSessionId = 7,
      CreatedAt = new DateTime(2026, 8, 28, 6, 0, 0, DateTimeKind.Utc),
      StakeAmount = 10m,
      TotalOdds = 2m,
      PotentialPayout = 20m,
      BetStatus = BetStatus.Pending,
      BetStatusEntity = new BetStatusEntity { Id = 1, Name = "Pending" },
      DailyPick = new DailyPick
      {
        BetSlipId = id,
        RiskLevelId = (int)risk,
        SlipDate = slipDate,
        RiskLevel = new BetRiskLevelEntity { Id = (int)risk, Name = risk.ToString() }
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
  }
}
