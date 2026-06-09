namespace NoMoreBets.Domain.Bankrolls;

public static class BankrollEntryNames
{
  public const string Salary = "Salary";
  public const string BetWin = "Bet win";
  public const string BetStake = "Bet stake";
  public const string BetCancellationRefund = "Bet cancellation refund";

  public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
  {
    Salary,
    BetWin,
    BetStake,
    BetCancellationRefund,
  };
}
