using FluentAssertions;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Tests.Domain.Enums;

public class BettingEventOptionDisplayTests
{
  [Fact]
  public void GetDisplayName_MatchResult_Home_WithClubNames_UsesHomeClubName()
  {
    // Arrange
    const string home = "Legia Warszawa";
    const string away = "Lech Poznań";

    // Act
    var name = BettingEventOptionDisplay.GetDisplayName(BettingEventOption.MatchResult_Home, home, away);

    // Assert
    name.Should().Be(home);
  }

  [Fact]
  public void GetDisplayName_DoubleChance_HomeOrAway_FormatsBothClubs()
  {
    // Arrange
    const string home = "Legia Warszawa";
    const string away = "Lech Poznań";

    // Act
    var name = BettingEventOptionDisplay.GetDisplayName(BettingEventOption.DoubleChance_HomeOrAway, home, away);

    // Assert
    name.Should().Be("Legia Warszawa or Lech Poznań");
  }

  [Fact]
  public void GetDisplayName_Handicap_Home_Minus_1_IncludesHandicapAndHome()
  {
    // Arrange
    const string home = "Arsenal";
    const string away = "Chelsea";

    // Act
    var name = BettingEventOptionDisplay.GetDisplayName(BettingEventOption.Handicap_Home_Minus_1, home, away);

    // Assert
    name.Should().Be("Arsenal (-1)");
  }

  [Fact]
  public void GetDisplayName_TotalGoals_Over_2_5_ReturnsEnglishTotal()
  {
    // Act
    var name = BettingEventOptionDisplay.GetDisplayName(BettingEventOption.TotalGoals_Over_2_5, "H", "A");

    // Assert
    name.Should().Be("Over 2.5");
  }

  [Fact]
  public void GetDisplayName_WithNullClubNames_UsesFallbackHomeAway()
  {
    // Act
    var name = BettingEventOptionDisplay.GetDisplayName(BettingEventOption.MatchResult_Away, null, null);

    // Assert
    name.Should().Be("Away");
  }

  [Fact]
  public void GetDisplayName_WithWhitespaceClubNames_UsesFallback()
  {
    // Act
    var name = BettingEventOptionDisplay.GetDisplayName(BettingEventOption.DoubleChance_HomeOrDraw, "   ", "");

    // Assert
    name.Should().Be("Home or draw");
  }

  [Fact]
  public void GetDisplayName_MatchResult_Draw_ReturnsDraw()
  {
    // Act
    var name = BettingEventOptionDisplay.GetDisplayName(BettingEventOption.MatchResult_Draw, "H", "A");

    // Assert
    name.Should().Be("Draw");
  }

  [Fact]
  public void GetDisplayName_BothTeamsToScore_YesNo_ReturnsEnglish()
  {
    // Act & Assert
    BettingEventOptionDisplay.GetDisplayName(BettingEventOption.BothTeamsToScore_Yes, "H", "A").Should().Be("Yes");
    BettingEventOptionDisplay.GetDisplayName(BettingEventOption.BothTeamsToScore_No, "H", "A").Should().Be("No");
  }

  [Fact]
  public void GetDisplayName_Handicap_Draw_Minus_1_UsesDrawLabel()
  {
    // Act
    var name = BettingEventOptionDisplay.GetDisplayName(BettingEventOption.Handicap_Draw_Minus_1, "Arsenal", "Chelsea");

    // Assert
    name.Should().Be("Draw (-1)");
  }

  [Fact]
  public void GetDisplayName_CorrectScore_ReturnsScoreString()
  {
    // Act
    var name = BettingEventOptionDisplay.GetDisplayName(BettingEventOption.CorrectScore_2_1, "H", "A");

    // Assert
    name.Should().Be("2:1");
  }

  [Fact]
  public void GetDisplayName_CorrectScore_Other_ReturnsOther()
  {
    // Act
    var name = BettingEventOptionDisplay.GetDisplayName(BettingEventOption.CorrectScore_Other, "H", "A");

    // Assert
    name.Should().Be("Other");
  }

  [Fact]
  public void GetDisplayOrder_UnknownLabel_ReturnsMaxValue()
  {
    // Act
    var order = BettingEventOptionDisplay.GetDisplayOrder("UnknownFutureOption");

    // Assert
    order.Should().Be(int.MaxValue);
  }

  [Fact]
  public void GetDisplayOrder_KnownOption_ReturnsEnumValue()
  {
    // Act & Assert
    BettingEventOptionDisplay.GetDisplayOrder(BettingEventOption.TotalGoals_Over_2_5)
      .Should().Be((int)BettingEventOption.TotalGoals_Over_2_5);
    BettingEventOptionDisplay.GetDisplayOrder(nameof(BettingEventOption.TotalGoals_Over_2_5))
      .Should().Be((int)BettingEventOption.TotalGoals_Over_2_5);
  }

  [Fact]
  public void GetDisplayName_WhenValueIsNotDefined_ThrowsArgumentOutOfRangeException()
  {
    // Arrange
    const BettingEventOption invalid = (BettingEventOption)0;

    // Act
    var act = () => BettingEventOptionDisplay.GetDisplayName(invalid, "H", "A");

    // Assert
    act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("option");
  }
}
