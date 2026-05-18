using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Memories;

namespace NoMoreBets.Application.Memories.GetMemoriesPage;

public record GetMemoriesPageQuery(
  int Limit,
  DateTime? AfterUpdatedAtUtc,
  int? AfterId) : IRequest<Paged<MemoryListItem>>;

public sealed class GetMemoriesPageHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetMemoriesPageQuery, Paged<MemoryListItem>>
{
  public async Task<Paged<MemoryListItem>> Handle(
    GetMemoriesPageQuery request,
    CancellationToken cancellationToken)
  {
    var page = await unitOfWork.Memories
      .GetPageAsync(request.Limit, request.AfterUpdatedAtUtc, request.AfterId, cancellationToken)
      .ConfigureAwait(false);

    return PagedFactory.Create(
      page.Items,
      page.HasMore,
      item => item.UpdatedAt,
      item => item.Id);
  }
}
