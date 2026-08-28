namespace NoMoreBets.Domain.Betting;

public sealed record DailyPickSlipPage(IReadOnlyList<BetSlip> Items, bool HasMore);
