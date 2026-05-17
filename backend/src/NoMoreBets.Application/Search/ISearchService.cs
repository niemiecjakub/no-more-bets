using System.Threading;
using System.Threading.Tasks;
using NoMoreBets.Application.Search.SearchBasic;
using NoMoreBets.Application.Search.SearchLlmContext;
using NoMoreBets.Application.Search.SearchNews;

namespace NoMoreBets.Application.Search;

public interface ISearchService
{
  Task<SearchBasicResultDto> SearchBasicAsync(string q, SearchBasicOptions options, CancellationToken cancellationToken = default);

  Task<SearchNewsResultDto> SearchNewsAsync(string q, SearchNewsOptions options, CancellationToken cancellationToken = default);

  Task<SearchLlmContextResultDto> SearchLlmContextAsync(string q, SearchLlmContextOptions options, CancellationToken cancellationToken = default);
}
