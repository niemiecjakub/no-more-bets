namespace NoMoreBets.Domain.Enums;

public static class BettingEventOptionDisplay
{
  public static string GetDisplayName(BettingEventOption option, string? homeClubName, string? awayClubName)
  {
    var h = string.IsNullOrWhiteSpace(homeClubName) ? "Home" : homeClubName.Trim();
    var a = string.IsNullOrWhiteSpace(awayClubName) ? "Away" : awayClubName.Trim();

    return option switch
    {
      BettingEventOption.DoubleChance_HomeOrAway => $"{h} or {a}",
      BettingEventOption.DoubleChance_HomeOrDraw => $"{h} or draw",
      BettingEventOption.DoubleChance_AwayOrDraw => $"{a} or draw",

      BettingEventOption.MatchResult_Home => h,
      BettingEventOption.MatchResult_Away => a,
      BettingEventOption.MatchResult_Draw => "Draw",

      BettingEventOption.BothTeamsToScore_Yes => "Yes",
      BettingEventOption.BothTeamsToScore_No => "No",

      BettingEventOption.TotalGoals_Over_0_5 => "Over 0.5",
      BettingEventOption.TotalGoals_Under_0_5 => "Under 0.5",
      BettingEventOption.TotalGoals_Over_1_5 => "Over 1.5",
      BettingEventOption.TotalGoals_Under_1_5 => "Under 1.5",
      BettingEventOption.TotalGoals_Over_2_5 => "Over 2.5",
      BettingEventOption.TotalGoals_Under_2_5 => "Under 2.5",
      BettingEventOption.TotalGoals_Over_3_5 => "Over 3.5",
      BettingEventOption.TotalGoals_Under_3_5 => "Under 3.5",
      BettingEventOption.TotalGoals_Over_4_5 => "Over 4.5",
      BettingEventOption.TotalGoals_Under_4_5 => "Under 4.5",
      BettingEventOption.TotalGoals_Over_5_5 => "Over 5.5",
      BettingEventOption.TotalGoals_Under_5_5 => "Under 5.5",

      BettingEventOption.Handicap_Home_Minus_4 => $"{h} (-4)",
      BettingEventOption.Handicap_Draw_Minus_4 => "Draw (-4)",
      BettingEventOption.Handicap_Away_Plus_4 => $"{a} (+4)",
      BettingEventOption.Handicap_Home_Minus_3 => $"{h} (-3)",
      BettingEventOption.Handicap_Draw_Minus_3 => "Draw (-3)",
      BettingEventOption.Handicap_Away_Plus_3 => $"{a} (+3)",
      BettingEventOption.Handicap_Home_Minus_2 => $"{h} (-2)",
      BettingEventOption.Handicap_Draw_Minus_2 => "Draw (-2)",
      BettingEventOption.Handicap_Away_Plus_2 => $"{a} (+2)",
      BettingEventOption.Handicap_Home_Minus_1 => $"{h} (-1)",
      BettingEventOption.Handicap_Draw_Minus_1 => "Draw (-1)",
      BettingEventOption.Handicap_Away_Plus_1 => $"{a} (+1)",
      BettingEventOption.Handicap_Home_Plus_1 => $"{h} (+1)",
      BettingEventOption.Handicap_Draw_Plus_1 => "Draw (+1)",
      BettingEventOption.Handicap_Away_Minus_1 => $"{a} (-1)",
      BettingEventOption.Handicap_Home_Plus_2 => $"{h} (+2)",
      BettingEventOption.Handicap_Draw_Plus_2 => "Draw (+2)",
      BettingEventOption.Handicap_Away_Minus_2 => $"{a} (-2)",
      BettingEventOption.Handicap_Home_Plus_3 => $"{h} (+3)",
      BettingEventOption.Handicap_Draw_Plus_3 => "Draw (+3)",
      BettingEventOption.Handicap_Away_Minus_3 => $"{a} (-3)",

      BettingEventOption.CorrectScore_0_0 => "0:0",
      BettingEventOption.CorrectScore_0_1 => "0:1",
      BettingEventOption.CorrectScore_0_2 => "0:2",
      BettingEventOption.CorrectScore_0_3 => "0:3",
      BettingEventOption.CorrectScore_0_4 => "0:4",
      BettingEventOption.CorrectScore_1_0 => "1:0",
      BettingEventOption.CorrectScore_1_1 => "1:1",
      BettingEventOption.CorrectScore_1_2 => "1:2",
      BettingEventOption.CorrectScore_1_3 => "1:3",
      BettingEventOption.CorrectScore_1_4 => "1:4",
      BettingEventOption.CorrectScore_2_0 => "2:0",
      BettingEventOption.CorrectScore_2_1 => "2:1",
      BettingEventOption.CorrectScore_2_2 => "2:2",
      BettingEventOption.CorrectScore_2_3 => "2:3",
      BettingEventOption.CorrectScore_2_4 => "2:4",
      BettingEventOption.CorrectScore_3_0 => "3:0",
      BettingEventOption.CorrectScore_3_1 => "3:1",
      BettingEventOption.CorrectScore_3_2 => "3:2",
      BettingEventOption.CorrectScore_3_3 => "3:3",
      BettingEventOption.CorrectScore_4_0 => "4:0",
      BettingEventOption.CorrectScore_4_1 => "4:1",
      BettingEventOption.CorrectScore_4_2 => "4:2",
      BettingEventOption.CorrectScore_4_3 => "4:3",
      BettingEventOption.CorrectScore_4_4 => "4:4",
      BettingEventOption.CorrectScore_Other => "Other",
      _ => throw new ArgumentOutOfRangeException(nameof(option), option, "Unknown betting event option."),
    };
  }
}
