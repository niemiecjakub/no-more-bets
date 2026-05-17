namespace NoMoreBets.Domain.Enums;

public enum BankrollFlow
{
  In,
  Out
}

public static class BankrollFlowExtensions
{
  public const string InCode = "IN";
  public const string OutCode = "OUT";

  public static string ToStorageCode(this BankrollFlow flow)
  {
    return flow switch
    {
      BankrollFlow.In => InCode,
      BankrollFlow.Out => OutCode,
      _ => throw new ArgumentOutOfRangeException(nameof(flow), flow, null)
    };
  }

  public static BankrollFlow FromStorageCode(string code)
  {
    return code switch
    {
      InCode => BankrollFlow.In,
      OutCode => BankrollFlow.Out,
      _ => throw new ArgumentException($"Invalid bankroll flow code: '{code}'.", nameof(code))
    };
  }
}
