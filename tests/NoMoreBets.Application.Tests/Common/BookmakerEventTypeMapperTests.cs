using FluentAssertions;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Tests.Common;

public class BookmakerEventTypeMapperTests
{
  [Fact]
  public void Map_NullTitle_ReturnsNull()
  {
    // Arrange
    string? title = null;

    // Act
    var result = BookmakerEventTypeMapper.Map(title);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void Map_EmptyString_ReturnsNull()
  {
    // Arrange
    var title = "";

    // Act
    var result = BookmakerEventTypeMapper.Map(title);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void Map_WhiteSpaceTitle_ReturnsNull()
  {
    // Arrange
    var title = "   ";

    // Act
    var result = BookmakerEventTypeMapper.Map(title);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void Map_ExactMatchDisplayName_ReturnsCorrectType()
  {
    // Arrange
    var title = "Wynik meczu (z wyłączeniem dogrywki)";

    // Act
    var result = BookmakerEventTypeMapper.Map(title);

    // Assert
    result.Should().Be(BettingEventType.MatchResult);
  }

  [Fact]
  public void Map_ExactMatchCaseInsensitive_ReturnsCorrectType()
  {
    // Arrange - Betclic uses "Podwójna Szansa", Display is "Podwójna szansa"
    var title = "Podwójna Szansa";

    // Act
    var result = BookmakerEventTypeMapper.Map(title);

    // Assert
    result.Should().Be(BettingEventType.DoubleChance);
  }

  [Fact]
  public void Map_PrefixMatchWithTeamSuffix_ReturnsCorrectType()
  {
    // Arrange - "Liczba goli - Wolverhampton"
    var title = "Liczba goli - Wolverhampton";

    // Act
    var result = BookmakerEventTypeMapper.Map(title);

    // Assert
    result.Should().Be(BettingEventType.TeamGoals);
  }

  [Fact]
  public void Map_PrefixMatchPlayerOrSubToScore_ReturnsCorrectType()
  {
    // Arrange
    var title = "Zawodnik lub jego zmiennik strzeli gola (90 min) - Aston Villa";

    // Act
    var result = BookmakerEventTypeMapper.Map(title);

    // Assert
    result.Should().Be(BettingEventType.PlayerOrSubToScore);
  }

  [Fact]
  public void Map_PrefixMatchGoalscorer_ReturnsCorrectType()
  {
    // Arrange
    var title = "Strzelec - Aston Villa";

    // Act
    var result = BookmakerEventTypeMapper.Map(title);

    // Assert
    result.Should().Be(BettingEventType.Goalscorer);
  }

  [Theory]
  [InlineData("Gole Powyżej/Poniżej", BettingEventType.OverUnderGoals)]
  [InlineData("Oba zespoły strzelą gola", BettingEventType.BothTeamsToScore)]
  [InlineData("Którykolwiek zawodnik strzeli gola", BettingEventType.AnyPlayerToScore)]
  [InlineData("Handicap", BettingEventType.Handicap)]
  [InlineData("Dokładny wynik", BettingEventType.ExactScore)]
  public void Map_SampleTitlesFromBetclic_ReturnsExpectedType(string title, BettingEventType expected)
  {
    // Act
    var result = BookmakerEventTypeMapper.Map(title);

    // Assert
    result.Should().Be(expected);
  }

  [Fact]
  public void Map_WhenTitleStartsWithExactScoreDisplayNameWithNonTeamSuffix_ReturnsNull()
  {
    // Arrange - "Dokładny wynik 1. połowa" must NOT match "Dokładny wynik" (only exact or " - Team" suffix)
    var title = "Dokładny wynik 1. połowa";

    // Act
    var result = BookmakerEventTypeMapper.Map(title);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public void Map_WhenTitleStartsWithBothTeamsToScoreDisplayNameWithNonTeamSuffix_ReturnsNull()
  {
    // Arrange - "Oba zespoły strzelą gola lub ..." must NOT match "Oba zespoły strzelą gola" (only exact or " - Team" suffix)
    var title = "Oba zespoły strzelą gola lub coś innego";

    // Act
    var result = BookmakerEventTypeMapper.Map(title);

    // Assert
    result.Should().BeNull();
  }

  [Theory]
  [InlineData("1. połowa Wynik")]
  [InlineData("2. połowa Wynik")]
  [InlineData("Unknown market title")]
  public void Map_UnmappedTitle_ReturnsNull(string title)
  {
    // Act
    var result = BookmakerEventTypeMapper.Map(title);

    // Assert
    result.Should().BeNull();
  }
}
