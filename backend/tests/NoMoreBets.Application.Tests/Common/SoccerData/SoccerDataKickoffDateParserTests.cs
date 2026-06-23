using FluentAssertions;
using NoMoreBets.Application.Common.SoccerData;

namespace NoMoreBets.Application.Tests.Common.SoccerData;

public class SoccerDataKickoffDateParserTests
{
  [Fact]
  public void TryParse_WithDateAndTime_AppliesTwoHourOffset()
  {
    // Arrange
  // Act
    var result = SoccerDataKickoffDateParser.TryParse("15/08/2025", "19:00", out var kickoffUtc);

    // Assert
    result.Should().BeTrue();
    kickoffUtc.Should().Be(new DateTime(2025, 8, 15, 21, 0, 0, DateTimeKind.Utc));
  }

  [Fact]
  public void TryParse_WithDateOnly_DefaultsToMidnightPlusOffset()
  {
    // Arrange
  // Act
    var result = SoccerDataKickoffDateParser.TryParse("15/08/2025", null, out var kickoffUtc);

    // Assert
    result.Should().BeTrue();
    kickoffUtc.Should().Be(new DateTime(2025, 8, 15, 2, 0, 0, DateTimeKind.Utc));
  }

  [Fact]
  public void TryParse_WithInvalidDate_ReturnsFalse()
  {
    // Arrange
  // Act
    var result = SoccerDataKickoffDateParser.TryParse("not-a-date", "19:00", out var kickoffUtc);

    // Assert
    result.Should().BeFalse();
    kickoffUtc.Should().Be(default);
  }

  [Fact]
  public void TryParse_WithEmptyDate_ReturnsFalse()
  {
    // Arrange
  // Act
    var result = SoccerDataKickoffDateParser.TryParse("", "19:00", out var kickoffUtc);

    // Assert
    result.Should().BeFalse();
    kickoffUtc.Should().Be(default);
  }
}
