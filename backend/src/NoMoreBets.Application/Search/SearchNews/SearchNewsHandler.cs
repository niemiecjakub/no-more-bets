using MediatR;

namespace NoMoreBets.Application.Search.SearchNews;

public record SearchNewsQuery(string Q, SearchNewsOptions Options) : IRequest<SearchNewsResultDto>;

public sealed class SearchNewsHandler(ISearchService searchService) : IRequestHandler<SearchNewsQuery, SearchNewsResultDto>
{
  public Task<SearchNewsResultDto> Handle(SearchNewsQuery request, CancellationToken cancellationToken)
  {
    return searchService.SearchNewsAsync(request.Q, request.Options, cancellationToken);
  }
}
