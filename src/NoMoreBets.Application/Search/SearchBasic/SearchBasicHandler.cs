using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NoMoreBets.Application.Search;

namespace NoMoreBets.Application.Search.SearchBasic;

public record SearchBasicQuery(string Q, SearchBasicOptions Options) : IRequest<SearchBasicResultDto>;

public sealed class SearchBasicHandler(ISearchService searchService) : IRequestHandler<SearchBasicQuery, SearchBasicResultDto>
{
  public Task<SearchBasicResultDto> Handle(SearchBasicQuery request, CancellationToken cancellationToken)
  {
    return searchService.SearchBasicAsync(request.Q, request.Options, cancellationToken);
  }
}
