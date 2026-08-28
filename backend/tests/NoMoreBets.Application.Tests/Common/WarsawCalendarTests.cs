using FluentAssertions;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Tests.Common;

public class WarsawCalendarTests
{
  [Fact]
  public void DateFromUtc_BeforeWarsawMidnight_StaysOnSameCalendarDay()
  {
    // Arrange — 21:00 UTC in August is 23:00 in Warsaw (UTC+2)
    var utc = new DateTime(2026, 8, 28, 21, 0, 0, DateTimeKind.Utc);

    // Act
    var date = WarsawCalendar.DateFromUtc(utc);

    // Assert
    date.Should().Be(new DateOnly(2026, 8, 28));
  }

  [Fact]
  public void DateFromUtc_AfterWarsawMidnight_RollsToNextCalendarDay()
  {
    // Arrange — 22:00 UTC in August is 00:00 in Warsaw
    var utc = new DateTime(2026, 8, 28, 22, 0, 0, DateTimeKind.Utc);

    // Act
    var date = WarsawCalendar.DateFromUtc(utc);

    // Assert
    date.Should().Be(new DateOnly(2026, 8, 29));
  }
}
