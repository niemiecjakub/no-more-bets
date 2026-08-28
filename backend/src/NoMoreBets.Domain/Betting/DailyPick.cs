namespace NoMoreBets.Domain.Betting;

public class DailyPick
{
  public const string TABLE_NAME = "DailyPick";

  public int BetSlipId { get; set; }
  public int RiskLevelId { get; set; }
  public DateOnly SlipDate { get; set; }

  public BetSlip BetSlip { get; set; } = null!;
  public BetRiskLevelEntity RiskLevel { get; set; } = null!;
}
