using FluentAssertions;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Tests.Domain.Enums;

public class BettingEventTypeDisplayTests
{
  public static readonly TheoryData<BettingEventType, string> ExpectedDisplayNames =
    new()
    {
      { BettingEventType.OverUnderGoals, "Over/Under Goals" },
      { BettingEventType.DoubleChance, "Double Chance" },
      { BettingEventType.BothTeamsToScore, "Both Teams to Score" },
      { BettingEventType.MatchResult, "Match Result (90 min)" },
      { BettingEventType.Handicap, "Handicap" },
      { BettingEventType.ExactScore, "Exact Score" },
    };

  [Theory]
  [MemberData(nameof(ExpectedDisplayNames))]
  public void GetDisplayName_ReturnsEnglishName(BettingEventType type, string expected)
  {
    // Act
    var name = BettingEventTypeDisplay.GetDisplayName(type);

    // Assert
    name.Should().Be(expected);
  }

  [Fact]
  public void GetDisplayName_WhenValueIsNotDefined_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    const BettingEventType invalid = (BettingEventType)2;

    // Act
    var act = () => BettingEventTypeDisplay.GetDisplayName(invalid);

    // Assert
    act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("type");
  }

  [Fact]
  public void DisplayOrder_FollowsMatchHistoryUiSequence()
  {
    BettingEventTypeDisplay.DisplayOrder.Should().ContainInOrder(
      BettingEventType.MatchResult,
      BettingEventType.DoubleChance,
      BettingEventType.BothTeamsToScore,
      BettingEventType.OverUnderGoals,
      BettingEventType.Handicap,
      BettingEventType.ExactScore);
  }

  [Fact]
  public void GetDisplayOrder_KnownTypes_ReturnsIndexInDisplayOrder()
  {
    for (var i = 0; i < BettingEventTypeDisplay.DisplayOrder.Count; i++)
      BettingEventTypeDisplay.GetDisplayOrder(BettingEventTypeDisplay.DisplayOrder[i]).Should().Be(i);
  }

  [Fact]
  public void GetDisplayOrder_WhenEnumValueUnknown_SortsLast()
  {
    BettingEventTypeDisplay.GetDisplayOrder((BettingEventType)999).Should().Be(int.MaxValue);
  }
}
