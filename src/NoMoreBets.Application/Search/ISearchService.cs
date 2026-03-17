using System.Threading;
using System.Threading.Tasks;

namespace NoMoreBets.Application.Search;

public interface ISearchService
{
  Task<SearchResultDto> SearchAsync(string q, SearchOptions options, CancellationToken cancellationToken = default);

  Task<SearchNewsResultDto> SearchNewsAsync(string q, SearchNewsOptions options, CancellationToken cancellationToken = default);

  Task<SearchLlmContextResultDto> SearchLlmContextAsync(string q, SearchLlmContextOptions options, CancellationToken cancellationToken = default);
}

