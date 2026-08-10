using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Bankroll.GetBankrollEntriesPage;

public record GetBankrollEntriesPageQuery(
  int Limit,
  DateTime? AfterCreatedAtUtc,
  int? AfterId,
  IReadOnlyCollection<string>? EntryNames = null,
  IReadOnlyList<string>? SeasonYears = null) : IRequest<Paged<BankrollEntryListItemDto>>;

public sealed class GetBankrollEntriesPageHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetBankrollEntriesPageQuery, Paged<BankrollEntryListItemDto>>
{
  public async Task<Paged<BankrollEntryListItemDto>> Handle(
    GetBankrollEntriesPageQuery request,
    CancellationToken cancellationToken)
  {
    var page = await unitOfWork.Bankroll
      .GetEntriesPageAsync(
        request.Limit,
        request.AfterCreatedAtUtc,
        request.AfterId,
        request.EntryNames,
        request.SeasonYears,
        cancellationToken)
      .ConfigureAwait(false);

    var items = BankrollEntriesPagination.MapRows(page.Items)
      .Select(item => new BankrollEntryListItemDto(
        item.Id,
        item.Name,
        item.Amount,
        item.Flow,
        item.Delta,
        item.CreatedAt,
        item.BetId))
      .ToList();

    return PagedFactory.Create(items, page.HasMore, item => item.CreatedAt, item => item.Id);
  }
}
