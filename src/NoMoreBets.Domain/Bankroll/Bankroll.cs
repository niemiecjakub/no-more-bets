using System.ComponentModel.DataAnnotations.Schema;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Domain.Bankrolls;

public class Bankroll
{
  public const int MaxNameLength = 200;

  private const string SalaryName = "Salary";
  private const decimal SalaryAmount = 8000m;
  private const string BetWinName = "Bet win";
  private const string BetCancellationRefundName = "Bet cancellation refund";

  private Bankroll()
  {
  }

  public int Id { get; private set; }
  public string Name { get; private set; } = null!;
  public decimal Amount { get; private set; }
  public string Flow { get; private set; } = null!;
  public int? BetId { get; private set; }
  public DateTime CreatedAt { get; private set; }

  public BetSlip? BetSlip { get; private set; }

  [NotMapped]
  public BankrollFlow Direction => BankrollFlowExtensions.FromStorageCode(Flow);

  public static Bankroll CreateSalary() => Create(SalaryName, SalaryAmount, BankrollFlow.In);

  public static Bankroll CreateBetWin(decimal potentialPayout, int betSlipId) =>
    Create(BetWinName, potentialPayout, BankrollFlow.In, betSlipId);

  public static Bankroll CreateBetCancellationRefund(BetSlip betSlip) =>
    Create(BetCancellationRefundName, betSlip.StakeAmount, BankrollFlow.In, betSlip.Id);

  public static Bankroll Create(string name, decimal amount, BankrollFlow flow, int? betId = null)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException("Name must not be empty.", nameof(name));
    }

    if (name.Length > MaxNameLength)
    {
      throw new ArgumentException($"Name must be at most {MaxNameLength} characters.", nameof(name));
    }

    if (amount <= 0)
    {
      throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
    }

    return new Bankroll
    {
      Name = name.Trim(),
      Amount = amount,
      Flow = flow.ToStorageCode(),
      BetId = betId,
      CreatedAt = DateTime.UtcNow
    };
  }
}
