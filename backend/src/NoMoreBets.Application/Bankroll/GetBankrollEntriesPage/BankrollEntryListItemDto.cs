namespace NoMoreBets.Application.Bankroll.GetBankrollEntriesPage;

public record BankrollEntryListItemDto(
  int Id,
  string Name,
  decimal Amount,
  string Flow,
  decimal Delta,
  DateTime CreatedAt,
  int? BetId);
