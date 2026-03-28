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
}
