using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Memories;

namespace NoMoreBets.Application.Memories.GetMemoriesPage;

public record GetMemoriesPageQuery(
  int Limit,
  DateTime? AfterUpdatedAtUtc,
  int? AfterId) : IRequest<PagedResponse<MemoryListItem>>;

public sealed class GetMemoriesPageHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetMemoriesPageQuery, PagedResponse<MemoryListItem>>
{
  public async Task<PagedResponse<MemoryListItem>> Handle(
    GetMemoriesPageQuery request,
    CancellationToken cancellationToken)
  {
    var page = await unitOfWork.Memories
      .GetPageAsync(request.Limit, request.AfterUpdatedAtUtc, request.AfterId, cancellationToken)
      .ConfigureAwait(false);

    return PagedResponseFactory.Create(
      page.Items,
      page.HasMore,
      item => item.UpdatedAt,
      item => item.Id);
  }
}
