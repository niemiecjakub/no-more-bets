namespace NoMoreBets.Domain.Enums;

public enum BettingEventOption
{
  DoubleChance_HomeOrAway = 1,
  DoubleChance_HomeOrDraw = 2,
  DoubleChance_AwayOrDraw = 3,

  MatchResult_Home = 4,
  MatchResult_Away = 5,
  MatchResult_Draw = 6,

  BothTeamsToScore_Yes = 7,
  BothTeamsToScore_No = 8,

  TotalGoals_Over_0_5 = 9,
  TotalGoals_Under_0_5 = 10,
  TotalGoals_Over_1_5 = 11,
  TotalGoals_Under_1_5 = 12,
  TotalGoals_Over_2_5 = 13,
  TotalGoals_Under_2_5 = 14,
  TotalGoals_Over_3_5 = 15,
  TotalGoals_Under_3_5 = 16,
  TotalGoals_Over_4_5 = 17,
  TotalGoals_Under_4_5 = 18,
  TotalGoals_Over_5_5 = 19,
  TotalGoals_Under_5_5 = 20,

  Handicap_Home_Minus_4 = 31,
  Handicap_Draw_Minus_4 = 32,
  Handicap_Away_Plus_4 = 33,

  Handicap_Home_Minus_3 = 34,
  Handicap_Draw_Minus_3 = 35,
  Handicap_Away_Plus_3 = 36,

  Handicap_Home_Minus_2 = 37,
  Handicap_Draw_Minus_2 = 38,
  Handicap_Away_Plus_2 = 39,

  Handicap_Home_Minus_1 = 40,
  Handicap_Draw_Minus_1 = 41,
  Handicap_Away_Plus_1 = 42,

  Handicap_Home_Plus_1 = 43,
  Handicap_Draw_Plus_1 = 44,
  Handicap_Away_Minus_1 = 45,

  Handicap_Home_Plus_2 = 46,
  Handicap_Draw_Plus_2 = 47,
  Handicap_Away_Minus_2 = 48,

  Handicap_Home_Plus_3 = 49,
  Handicap_Draw_Plus_3 = 50,
  Handicap_Away_Minus_3 = 51,

  CorrectScore_0_0 = 52,
  CorrectScore_0_1 = 53,
  CorrectScore_0_2 = 54,
  CorrectScore_0_3 = 55,
  CorrectScore_0_4 = 56,

  CorrectScore_1_0 = 57,
  CorrectScore_1_1 = 58,
  CorrectScore_1_2 = 59,
  CorrectScore_1_3 = 60,
  CorrectScore_1_4 = 61,

  CorrectScore_2_0 = 62,
  CorrectScore_2_1 = 63,
  CorrectScore_2_2 = 64,
  CorrectScore_2_3 = 65,
  CorrectScore_2_4 = 66,

  CorrectScore_3_0 = 67,
  CorrectScore_3_1 = 68,
  CorrectScore_3_2 = 69,
  CorrectScore_3_3 = 70,

  CorrectScore_4_0 = 71,
  CorrectScore_4_1 = 72,
  CorrectScore_4_2 = 73,
  CorrectScore_4_3 = 74,
  CorrectScore_4_4 = 75,

  CorrectScore_Other = 76
}
