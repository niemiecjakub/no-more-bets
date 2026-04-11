using FluentAssertions;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Tests.Betting;

public class BettingSelectionOutcomeEvaluatorTests
{
  [Fact]
  public void ResolveBetStatus_CoversEveryBettingEventOption_WithoutThrowing()
  {
    foreach (BettingEventOption option in Enum.GetValues<BettingEventOption>())
    {
      var act = () => BettingSelectionOutcomeEvaluator.ResolveBetStatus(option, 2, 1);
      act.Should().NotThrow();
    }
  }

  [Theory]
  [InlineData(BettingEventOption.MatchResult_Home, 2, 1, BetStatus.Won)]
  [InlineData(BettingEventOption.MatchResult_Home, 1, 1, BetStatus.Lost)]
  [InlineData(BettingEventOption.MatchResult_Away, 0, 1, BetStatus.Won)]
  [InlineData(BettingEventOption.MatchResult_Draw, 2, 2, BetStatus.Won)]
  public void MatchResult(BettingEventOption option, int home, int away, BetStatus expected) =>
    BettingSelectionOutcomeEvaluator.ResolveBetStatus(option, home, away).Should().Be(expected);

  [Theory]
  [InlineData(BettingEventOption.DoubleChance_HomeOrDraw, 0, 1, BetStatus.Lost)]
  [InlineData(BettingEventOption.DoubleChance_HomeOrDraw, 1, 1, BetStatus.Won)]
  [InlineData(BettingEventOption.DoubleChance_HomeOrAway, 1, 1, BetStatus.Lost)]
  [InlineData(BettingEventOption.DoubleChance_HomeOrAway, 2, 1, BetStatus.Won)]
  [InlineData(BettingEventOption.DoubleChance_AwayOrDraw, 2, 0, BetStatus.Lost)]
  [InlineData(BettingEventOption.DoubleChance_AwayOrDraw, 1, 2, BetStatus.Won)]
  public void DoubleChance(BettingEventOption option, int home, int away, BetStatus expected) =>
    BettingSelectionOutcomeEvaluator.ResolveBetStatus(option, home, away).Should().Be(expected);

  [Theory]
  [InlineData(BettingEventOption.BothTeamsToScore_Yes, 1, 0, BetStatus.Lost)]
  [InlineData(BettingEventOption.BothTeamsToScore_Yes, 1, 1, BetStatus.Won)]
  [InlineData(BettingEventOption.BothTeamsToScore_No, 0, 3, BetStatus.Won)]
  [InlineData(BettingEventOption.BothTeamsToScore_No, 1, 1, BetStatus.Lost)]
  public void BothTeamsToScore(BettingEventOption option, int home, int away, BetStatus expected) =>
    BettingSelectionOutcomeEvaluator.ResolveBetStatus(option, home, away).Should().Be(expected);

  [Theory]
  [InlineData(BettingEventOption.TotalGoals_Over_2_5, 2, 1, BetStatus.Won)]
  [InlineData(BettingEventOption.TotalGoals_Over_2_5, 1, 1, BetStatus.Lost)]
  [InlineData(BettingEventOption.TotalGoals_Under_2_5, 1, 1, BetStatus.Won)]
  [InlineData(BettingEventOption.TotalGoals_Under_2_5, 2, 1, BetStatus.Lost)]
  [InlineData(BettingEventOption.TotalGoals_Over_0_5, 0, 0, BetStatus.Lost)]
  [InlineData(BettingEventOption.TotalGoals_Under_0_5, 0, 0, BetStatus.Won)]
  public void TotalGoals(BettingEventOption option, int home, int away, BetStatus expected) =>
    BettingSelectionOutcomeEvaluator.ResolveBetStatus(option, home, away).Should().Be(expected);

  [Fact]
  public void Handicap_Minus1_HomeWins_WhenHomeBeatsAwayByTwo()
  {
    BettingSelectionOutcomeEvaluator
      .ResolveBetStatus(BettingEventOption.Handicap_Home_Minus_1, 3, 1)
      .Should()
      .Be(BetStatus.Won);
  }

  [Fact]
  public void Handicap_Minus1_Draw_WhenHomeWinsByExactlyOne()
  {
    BettingSelectionOutcomeEvaluator
      .ResolveBetStatus(BettingEventOption.Handicap_Draw_Minus_1, 2, 1)
      .Should()
      .Be(BetStatus.Won);
  }

  [Fact]
  public void Handicap_Minus1_AwayCovers_WhenDraw()
  {
    BettingSelectionOutcomeEvaluator
      .ResolveBetStatus(BettingEventOption.Handicap_Away_Plus_1, 1, 1)
      .Should()
      .Be(BetStatus.Won);
  }

  [Fact]
  public void CorrectScore_Exact_WinsOnlyOnExactLine()
  {
    BettingSelectionOutcomeEvaluator
      .ResolveBetStatus(BettingEventOption.CorrectScore_2_1, 2, 1)
      .Should()
      .Be(BetStatus.Won);
    BettingSelectionOutcomeEvaluator
      .ResolveBetStatus(BettingEventOption.CorrectScore_2_1, 2, 2)
      .Should()
      .Be(BetStatus.Lost);
  }

  [Theory]
  [InlineData(5, 5)]
  [InlineData(10, 0)]
  public void CorrectScore_Other_WinsWhenNotListed(int home, int away)
  {
    BettingSelectionOutcomeEvaluator
      .ResolveBetStatus(BettingEventOption.CorrectScore_Other, home, away)
      .Should()
      .Be(BetStatus.Won);
  }

  [Fact]
  public void CorrectScore_Other_LosesOnListedScore()
  {
    BettingSelectionOutcomeEvaluator
      .ResolveBetStatus(BettingEventOption.CorrectScore_Other, 2, 2)
      .Should()
      .Be(BetStatus.Lost);
  }
}
