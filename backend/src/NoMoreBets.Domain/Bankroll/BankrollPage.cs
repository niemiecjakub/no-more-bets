namespace NoMoreBets.Domain.Bankrolls;

public sealed record BankrollPage(IReadOnlyList<BankrollEntryRow> Items, bool HasMore);

public sealed record BankrollEntryRow(
  int Id,
  string Name,
  decimal Amount,
  string Flow,
  DateTime CreatedAt,
  int? BetId);
