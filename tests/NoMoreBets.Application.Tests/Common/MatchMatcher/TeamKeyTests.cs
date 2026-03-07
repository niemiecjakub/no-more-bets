using FluentAssertions;
using NoMoreBets.Application.Common.MatchMatcher;

namespace NoMoreBets.Application.Tests.Common.MatchMatcher;

public class TeamKeyTests
{
  [Fact]
  public void Constructor_WhenHomeNull_NormalizesFirstToEmpty()
  {
    // Act
    var key = new TeamKey(null!, "Chelsea");

    // Assert
    key.First.Should().Be("");
    key.Second.Should().Be("chelsea");
  }

  [Fact]
  public void Constructor_WhenAwayNull_NormalizesSecondToEmpty()
  {
    // Act
    var key = new TeamKey("Arsenal", null!);

    // Assert
    key.First.Should().Be("");
    key.Second.Should().Be("arsenal");
  }

  [Fact]
  public void Constructor_WhenOrderReversed_ProducesSameFirstAndSecond()
  {
    // Act
    var key1 = new TeamKey("Arsenal", "Chelsea");
    var key2 = new TeamKey("Chelsea", "Arsenal");

    // Assert: both normalize to first="arsenal", second="chelsea"
    key1.First.Should().Be("arsenal");
    key1.Second.Should().Be("chelsea");
    key2.First.Should().Be("arsenal");
    key2.Second.Should().Be("chelsea");
  }

  [Fact]
  public void Equals_WhenReversedOrder_ReturnsTrue()
  {
    // Arrange
    var key1 = new TeamKey("Arsenal", "Chelsea");
    var key2 = new TeamKey("Chelsea", "Arsenal");

    // Act & Assert
    key1.Should().Be(key2);
    key1.GetHashCode().Should().Be(key2.GetHashCode());
  }

  [Fact]
  public void ToSearchString_ReturnsExpectedFormat()
  {
    // Arrange
    var key = new TeamKey("Arsenal", "Chelsea");

    // Act
    var result = key.ToSearchString();

    // Assert
    result.Should().Be("arsenal vs chelsea");
  }
}
