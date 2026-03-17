using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Search;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SearchController : ControllerBase
{
  private readonly ISearchService _searchService;

  public SearchController(ISearchService searchService)
  {
    _searchService = searchService;
  }

  [HttpGet]
  public async Task<ActionResult<SearchResultDto>> Search(
    [FromQuery] string q,
    [FromQuery] SearchOptions options,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(q))
      return BadRequest("Query 'q' is required.");

    var result = await _searchService.SearchAsync(q, options, cancellationToken);
    return Ok(result);
  }

  [HttpGet("news")]
  public async Task<ActionResult<SearchNewsResultDto>> SearchNews(
    [FromQuery] string q,
    [FromQuery] SearchNewsOptions options,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(q))
      return BadRequest("Query 'q' is required.");

    var result = await _searchService.SearchNewsAsync(q, options, cancellationToken);
    return Ok(result);
  }

  [HttpGet("llmcontext")]
  public async Task<ActionResult<SearchLlmContextResultDto>> SearchLlmContext(
    [FromQuery] string q,
    [FromQuery] SearchLlmContextOptions options,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(q))
      return BadRequest("Query 'q' is required.");

    var result = await _searchService.SearchLlmContextAsync(q, options, cancellationToken);
    return Ok(result);
  }
}

