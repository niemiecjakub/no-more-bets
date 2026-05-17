using System.ComponentModel.DataAnnotations;

namespace NoMoreBets.Domain.Enums;
public enum BettingEventType
{
  [Display(Name = "Gole Powyżej/Poniżej")]
  OverUnderGoals = 1,

  [Display(Name = "Podwójna szansa")]
  DoubleChance = 3,

  [Display(Name = "Oba zespoły strzelą gola")]
  BothTeamsToScore = 4,

  [Display(Name = "Wynik meczu (z wyłączeniem dogrywki)")]
  MatchResult = 5,

  [Display(Name = "Handicap")]
  Handicap = 11,

  [Display(Name = "Dokładny wynik")]
  ExactScore = 12
}
