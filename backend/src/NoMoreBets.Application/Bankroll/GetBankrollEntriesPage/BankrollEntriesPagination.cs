using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Bankroll.GetBankrollEntriesPage;

public record BankrollEntryListItem(
  int Id,
  string Name,
  decimal Amount,
  string Flow,
  decimal Delta,
  DateTime CreatedAt,
  int? BetId);

public static class BankrollEntriesPagination
{
  public static IReadOnlyList<BankrollEntryListItem> MapRows(IReadOnlyList<BankrollEntryRow> rows)
  {
    var items = new List<BankrollEntryListItem>(rows.Count);

    foreach (var row in rows)
    {
      var delta = row.Flow == BankrollFlowExtensions.InCode ? row.Amount : -row.Amount;
      var flow = row.Flow == BankrollFlowExtensions.InCode ? nameof(BankrollFlow.In) : nameof(BankrollFlow.Out);
      items.Add(new BankrollEntryListItem(
        row.Id,
        row.Name,
        row.Amount,
        flow,
        delta,
        row.CreatedAt,
        row.BetId));
    }

    return items;
  }
}
