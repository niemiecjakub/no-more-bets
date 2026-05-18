using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.AgentSessions.GetAgentSessionMessages;
using NoMoreBets.Application.AgentSessions.GetAgentSessionsPage;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api")]
public class AgentSessionsController(IMediator mediator) : ControllerBase
{
  [HttpGet("agent-sessions")]
  public async Task<ActionResult<Paged<AgentSessionListItemDto>>> GetAgentSessions(
    [FromQuery] int limit = 15,
    [FromQuery] DateTime? afterStartedAt = null,
    [FromQuery] int? afterId = null,
    [FromQuery] int? includeSessionId = null,
    CancellationToken cancellationToken = default)
  {
    limit = Math.Clamp(limit, 1, 100);

    if (afterStartedAt is null != afterId is null)
    {
      return BadRequest("afterStartedAt and afterId must both be provided or omitted.");
    }

    DateTime? afterStartedAtUtc = afterStartedAt is not null
      ? DateTimeQueryExtensions.ToUtc(afterStartedAt.Value)
      : null;

    var result = await mediator.Send(
      new GetAgentSessionsPageQuery(limit, afterStartedAtUtc, afterId, includeSessionId),
      cancellationToken);

    return Ok(result);
  }

  [HttpGet("agent-sessions/{sessionId:int}/messages")]
  public async Task<ActionResult<IReadOnlyList<AgentSessionMessageDto>>> GetAgentSessionMessages(
    int sessionId,
    CancellationToken cancellationToken = default)
  {
    var messages = await mediator
      .Send(new GetAgentSessionMessagesQuery(sessionId), cancellationToken)
      .ConfigureAwait(false);

    if (messages is null)
      return NotFound();

    return Ok(messages);
  }
}
