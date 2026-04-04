using NoMoreBets.Domain.Betting;

namespace NoMoreBets.Domain.Bankrolls;

public class Bankroll
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public decimal Amount { get; set; }
  public string Flow { get; set; } = string.Empty;
  public int? BetId { get; set; }
  public DateTime CreatedAt { get; set; }

  public BetSlip? BetSlip { get; set; }
}
