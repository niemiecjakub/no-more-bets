using FluentAssertions;
using NoMoreBets.Application.Betting.Common;
using NoMoreBets.Domain.Betting;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Betting.GetBetSlips;

public class BetSlipListItemMapperTests
{
  [Fact]
  public void ToListItem_WhenNoDailyPick_LeavesRiskFieldsNull()
  {
    // Arrange
    var slip = CreateSlip();

    // Act
    var item = BetSlipListItemMapper.ToListItem(slip);

    // Assert
    item.RiskLevelId.Should().BeNull();
    item.RiskLevelName.Should().BeNull();
    item.SlipDate.Should().BeNull();
  }

  [Fact]
  public void ToListItem_WhenDailyPickPresent_MapsRiskAndSlipDate()
  {
    // Arrange
    var slipDate = new DateOnly(2026, 8, 28);
    var slip = CreateSlip();
    slip.DailyPick = new DailyPick
    {
      BetSlipId = slip.Id,
      RiskLevelId = (int)BetRiskLevel.High,
      SlipDate = slipDate,
      RiskLevel = new BetRiskLevelEntity { Id = (int)BetRiskLevel.High, Name = nameof(BetRiskLevel.High) }
    };

    // Act
    var item = BetSlipListItemMapper.ToListItem(slip);

    // Assert
    item.RiskLevelId.Should().Be((int)BetRiskLevel.High);
    item.RiskLevelName.Should().Be("High");
    item.SlipDate.Should().Be(slipDate);
  }

  private static BetSlip CreateSlip()
  {
    var match = new Match
    {
      Id = 9,
      HomeClub = new ClubEntity { Name = "Arsenal", Slug = "arsenal" },
      AwayClub = new ClubEntity { Name = "Chelsea", Slug = "chelsea" }
    };

    return new BetSlip
    {
      Id = 1,
      CreatedAt = new DateTime(2026, 8, 28, 6, 0, 0, DateTimeKind.Utc),
      StakeAmount = 10m,
      TotalOdds = 2.5m,
      PotentialPayout = 25m,
      BetStatus = BetStatus.Pending,
      BetStatusEntity = new BetStatusEntity { Id = (int)BetStatus.Pending, Name = nameof(BetStatus.Pending) },
      Selections =
      [
        new BetSelection
        {
          Id = 1,
          MatchId = 9,
          BetEventType = BettingEventType.MatchResult,
          BetEventOption = BettingEventOption.MatchResult_Home,
          OddsAtPlacement = 2.5m,
          BetStatus = BetStatus.Pending,
          BetStatusEntity = new BetStatusEntity { Id = (int)BetStatus.Pending, Name = nameof(BetStatus.Pending) },
          Match = match
        }
      ]
    };
  }
}
