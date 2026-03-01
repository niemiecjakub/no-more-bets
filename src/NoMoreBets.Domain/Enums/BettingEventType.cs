using System.ComponentModel.DataAnnotations;

namespace NoMoreBets.Domain.Enums;
public enum BettingEventType
{
  [Display(Name = "Gole Powyżej/Poniżej")]
  OverUnderGoals = 1,

  [Display(Name = "Liczba goli")]
  TeamGoals = 2,

  [Display(Name = "Podwójna szansa")]
  DoubleChance = 3,

  [Display(Name = "Oba zespoły strzelą gola")]
  BothTeamsToScore = 4,

  [Display(Name = "Wynik meczu (z wyłączeniem dogrywki)")]
  MatchResult = 5,

  [Display(Name = "Która drużyna strzeli pierwszego gola?")]
  FirstTeamToScore = 6,

  [Display(Name = "Zawodnik lub jego zmiennik strzeli gola (90 min)")]
  PlayerOrSubToScore = 7,

  [Display(Name = "Strzelec")]
  Goalscorer = 8,

  [Display(Name = "Zawodnik strzeli gola lub zaliczy asystę (90 min)")]
  PlayerGoalOrAssist = 9,

  [Display(Name = "Którykolwiek zawodnik strzeli gola")]
  AnyPlayerToScore = 10,

  [Display(Name = "Handicap")]
  Handicap = 11,

  [Display(Name = "Dokładny wynik")]
  ExactScore = 12
}
