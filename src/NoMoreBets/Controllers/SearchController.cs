using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Search.SearchBasic;
using NoMoreBets.Application.Search.SearchLlmContext;
using NoMoreBets.Application.Search.SearchNews;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SearchController : ControllerBase
{
  private readonly IMediator _mediator;

  public SearchController(IMediator mediator)
  {
    _mediator = mediator;
  }

  [HttpGet]
  public async Task<ActionResult<SearchBasicResultDto>> Search(
    [FromQuery] string q,
    [FromQuery] SearchBasicOptions options,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(q))
      return BadRequest("Query 'q' is required.");

    var result = await _mediator.Send(new SearchBasicQuery(q, options ?? new SearchBasicOptions()), cancellationToken);
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

    var result = await _mediator.Send(new SearchNewsQuery(q, options ?? new SearchNewsOptions()), cancellationToken);
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

    var result = await _mediator.Send(new SearchLlmContextQuery(q, options ?? new SearchLlmContextOptions()), cancellationToken);
    return Ok(result);
  }
}
