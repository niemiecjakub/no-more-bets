using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Memories.GetMemoriesPage;
using NoMoreBets.Domain.Memories;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api")]
public class MemoriesController(IMediator mediator) : ControllerBase
{
  [HttpGet("memories")]
  public async Task<ActionResult<Paged<MemoryListItem>>> GetMemories(
    [FromQuery] int limit = 15,
    [FromQuery] DateTime? afterUpdatedAt = null,
    [FromQuery] int? afterId = null,
    CancellationToken cancellationToken = default)
  {
    limit = Math.Clamp(limit, 1, 100);

    if (afterUpdatedAt is null != afterId is null)
    {
      return BadRequest("afterUpdatedAt and afterId must both be provided or omitted.");
    }

    DateTime? afterUpdatedAtUtc = afterUpdatedAt is not null
      ? DateTimeQueryExtensions.ToUtc(afterUpdatedAt.Value)
      : null;

    var result = await mediator.Send(
      new GetMemoriesPageQuery(limit, afterUpdatedAtUtc, afterId),
      cancellationToken).ConfigureAwait(false);

    return Ok(result);
  }
}
