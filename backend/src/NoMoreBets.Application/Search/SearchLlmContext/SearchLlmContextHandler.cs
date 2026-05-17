using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NoMoreBets.Application.Search;

namespace NoMoreBets.Application.Search.SearchLlmContext;

public record SearchLlmContextQuery(string Q, SearchLlmContextOptions Options) : IRequest<SearchLlmContextResultDto>;

public sealed class SearchLlmContextHandler(ISearchService searchService) : IRequestHandler<SearchLlmContextQuery, SearchLlmContextResultDto>
{
  public Task<SearchLlmContextResultDto> Handle(SearchLlmContextQuery request, CancellationToken cancellationToken)
  {
    return searchService.SearchLlmContextAsync(request.Q, request.Options, cancellationToken);
  }
}
