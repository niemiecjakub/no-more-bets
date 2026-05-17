using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Domain.Betting;

/// <summary>
/// Resolves <see cref="BetStatus.Won"/> or <see cref="BetStatus.Lost"/> for a <see cref="BettingEventOption"/> given full-time goals (90 min).
/// </summary>
public static class BettingSelectionOutcomeEvaluator
{
  private static readonly HashSet<(int Home, int Away)> ListedCorrectScores =
  [
    (0, 0), (0, 1), (0, 2), (0, 3), (0, 4),
    (1, 0), (1, 1), (1, 2), (1, 3), (1, 4),
    (2, 0), (2, 1), (2, 2), (2, 3), (2, 4),
    (3, 0), (3, 1), (3, 2), (3, 3),
    (4, 0), (4, 1), (4, 2), (4, 3), (4, 4)
  ];

  public static BetStatus ResolveBetStatus(BettingEventOption option, int homeGoals, int awayGoals) =>
    EvaluatesAsWin(option, homeGoals, awayGoals) ? BetStatus.Won : BetStatus.Lost;

  private static bool EvaluatesAsWin(BettingEventOption option, int homeGoals, int awayGoals)
  {
    return option switch
    {
      BettingEventOption.DoubleChance_HomeOrAway => homeGoals != awayGoals,
      BettingEventOption.DoubleChance_HomeOrDraw => homeGoals >= awayGoals,
      BettingEventOption.DoubleChance_AwayOrDraw => awayGoals >= homeGoals,

      BettingEventOption.MatchResult_Home => homeGoals > awayGoals,
      BettingEventOption.MatchResult_Away => awayGoals > homeGoals,
      BettingEventOption.MatchResult_Draw => homeGoals == awayGoals,

      BettingEventOption.BothTeamsToScore_Yes => homeGoals > 0 && awayGoals > 0,
      BettingEventOption.BothTeamsToScore_No => homeGoals == 0 || awayGoals == 0,

      BettingEventOption.TotalGoals_Over_0_5 => TotalGoals(homeGoals, awayGoals) > 0.5m,
      BettingEventOption.TotalGoals_Under_0_5 => TotalGoals(homeGoals, awayGoals) < 0.5m,
      BettingEventOption.TotalGoals_Over_1_5 => TotalGoals(homeGoals, awayGoals) > 1.5m,
      BettingEventOption.TotalGoals_Under_1_5 => TotalGoals(homeGoals, awayGoals) < 1.5m,
      BettingEventOption.TotalGoals_Over_2_5 => TotalGoals(homeGoals, awayGoals) > 2.5m,
      BettingEventOption.TotalGoals_Under_2_5 => TotalGoals(homeGoals, awayGoals) < 2.5m,
      BettingEventOption.TotalGoals_Over_3_5 => TotalGoals(homeGoals, awayGoals) > 3.5m,
      BettingEventOption.TotalGoals_Under_3_5 => TotalGoals(homeGoals, awayGoals) < 3.5m,
      BettingEventOption.TotalGoals_Over_4_5 => TotalGoals(homeGoals, awayGoals) > 4.5m,
      BettingEventOption.TotalGoals_Under_4_5 => TotalGoals(homeGoals, awayGoals) < 4.5m,
      BettingEventOption.TotalGoals_Over_5_5 => TotalGoals(homeGoals, awayGoals) > 5.5m,
      BettingEventOption.TotalGoals_Under_5_5 => TotalGoals(homeGoals, awayGoals) < 5.5m,

      BettingEventOption.Handicap_Home_Minus_4 => HomeAdjustedWins(homeGoals, awayGoals, -4),
      BettingEventOption.Handicap_Draw_Minus_4 => HomeAdjustedDraw(homeGoals, awayGoals, -4),
      BettingEventOption.Handicap_Away_Plus_4 => HomeAdjustedLoses(homeGoals, awayGoals, -4),

      BettingEventOption.Handicap_Home_Minus_3 => HomeAdjustedWins(homeGoals, awayGoals, -3),
      BettingEventOption.Handicap_Draw_Minus_3 => HomeAdjustedDraw(homeGoals, awayGoals, -3),
      BettingEventOption.Handicap_Away_Plus_3 => HomeAdjustedLoses(homeGoals, awayGoals, -3),

      BettingEventOption.Handicap_Home_Minus_2 => HomeAdjustedWins(homeGoals, awayGoals, -2),
      BettingEventOption.Handicap_Draw_Minus_2 => HomeAdjustedDraw(homeGoals, awayGoals, -2),
      BettingEventOption.Handicap_Away_Plus_2 => HomeAdjustedLoses(homeGoals, awayGoals, -2),

      BettingEventOption.Handicap_Home_Minus_1 => HomeAdjustedWins(homeGoals, awayGoals, -1),
      BettingEventOption.Handicap_Draw_Minus_1 => HomeAdjustedDraw(homeGoals, awayGoals, -1),
      BettingEventOption.Handicap_Away_Plus_1 => HomeAdjustedLoses(homeGoals, awayGoals, -1),

      BettingEventOption.Handicap_Home_Plus_1 => HomeAdjustedWins(homeGoals, awayGoals, 1),
      BettingEventOption.Handicap_Draw_Plus_1 => HomeAdjustedDraw(homeGoals, awayGoals, 1),
      BettingEventOption.Handicap_Away_Minus_1 => HomeAdjustedLoses(homeGoals, awayGoals, 1),

      BettingEventOption.Handicap_Home_Plus_2 => HomeAdjustedWins(homeGoals, awayGoals, 2),
      BettingEventOption.Handicap_Draw_Plus_2 => HomeAdjustedDraw(homeGoals, awayGoals, 2),
      BettingEventOption.Handicap_Away_Minus_2 => HomeAdjustedLoses(homeGoals, awayGoals, 2),

      BettingEventOption.Handicap_Home_Plus_3 => HomeAdjustedWins(homeGoals, awayGoals, 3),
      BettingEventOption.Handicap_Draw_Plus_3 => HomeAdjustedDraw(homeGoals, awayGoals, 3),
      BettingEventOption.Handicap_Away_Minus_3 => HomeAdjustedLoses(homeGoals, awayGoals, 3),

      BettingEventOption.CorrectScore_0_0 => homeGoals == 0 && awayGoals == 0,
      BettingEventOption.CorrectScore_0_1 => homeGoals == 0 && awayGoals == 1,
      BettingEventOption.CorrectScore_0_2 => homeGoals == 0 && awayGoals == 2,
      BettingEventOption.CorrectScore_0_3 => homeGoals == 0 && awayGoals == 3,
      BettingEventOption.CorrectScore_0_4 => homeGoals == 0 && awayGoals == 4,
      BettingEventOption.CorrectScore_1_0 => homeGoals == 1 && awayGoals == 0,
      BettingEventOption.CorrectScore_1_1 => homeGoals == 1 && awayGoals == 1,
      BettingEventOption.CorrectScore_1_2 => homeGoals == 1 && awayGoals == 2,
      BettingEventOption.CorrectScore_1_3 => homeGoals == 1 && awayGoals == 3,
      BettingEventOption.CorrectScore_1_4 => homeGoals == 1 && awayGoals == 4,
      BettingEventOption.CorrectScore_2_0 => homeGoals == 2 && awayGoals == 0,
      BettingEventOption.CorrectScore_2_1 => homeGoals == 2 && awayGoals == 1,
      BettingEventOption.CorrectScore_2_2 => homeGoals == 2 && awayGoals == 2,
      BettingEventOption.CorrectScore_2_3 => homeGoals == 2 && awayGoals == 3,
      BettingEventOption.CorrectScore_2_4 => homeGoals == 2 && awayGoals == 4,
      BettingEventOption.CorrectScore_3_0 => homeGoals == 3 && awayGoals == 0,
      BettingEventOption.CorrectScore_3_1 => homeGoals == 3 && awayGoals == 1,
      BettingEventOption.CorrectScore_3_2 => homeGoals == 3 && awayGoals == 2,
      BettingEventOption.CorrectScore_3_3 => homeGoals == 3 && awayGoals == 3,
      BettingEventOption.CorrectScore_4_0 => homeGoals == 4 && awayGoals == 0,
      BettingEventOption.CorrectScore_4_1 => homeGoals == 4 && awayGoals == 1,
      BettingEventOption.CorrectScore_4_2 => homeGoals == 4 && awayGoals == 2,
      BettingEventOption.CorrectScore_4_3 => homeGoals == 4 && awayGoals == 3,
      BettingEventOption.CorrectScore_4_4 => homeGoals == 4 && awayGoals == 4,

      BettingEventOption.CorrectScore_Other => !ListedCorrectScores.Contains((homeGoals, awayGoals)),

      _ => throw new ArgumentOutOfRangeException(nameof(option), option, "Unsupported betting event option.")
    };
  }

  private static decimal TotalGoals(int homeGoals, int awayGoals) => homeGoals + awayGoals;

  /// <summary>3-way handicap: compare (home + adjustment) to away.</summary>
  private static bool HomeAdjustedWins(int home, int away, int adjustment) => home + adjustment > away;

  private static bool HomeAdjustedDraw(int home, int away, int adjustment) => home + adjustment == away;

  private static bool HomeAdjustedLoses(int home, int away, int adjustment) => home + adjustment < away;
}
