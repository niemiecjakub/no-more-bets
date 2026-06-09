using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.AgentSessions.GetAgentSessionMessages;
using NoMoreBets.Application.AgentSessions.GetAgentSessionsPage;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;

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
    [FromQuery] int[]? phaseIds = null,
    CancellationToken cancellationToken = default)
  {
    limit = Math.Clamp(limit, 1, 100);

    if (afterStartedAt is null != afterId is null)
    {
      return BadRequest("afterStartedAt and afterId must both be provided or omitted.");
    }

    IReadOnlyCollection<AgentSessionPhase>? phaseFilter = null;
    if (phaseIds is { Length: > 0 })
    {
      var parsedPhases = new List<AgentSessionPhase>(phaseIds.Length);
      foreach (var phaseId in phaseIds)
      {
        if (!Enum.IsDefined(typeof(AgentSessionPhase), phaseId))
          return BadRequest($"Invalid phaseIds value: {phaseId}.");

        parsedPhases.Add((AgentSessionPhase)phaseId);
      }

      phaseFilter = parsedPhases;
    }

    DateTime? afterStartedAtUtc = afterStartedAt is not null
      ? DateTimeQueryExtensions.ToUtc(afterStartedAt.Value)
      : null;

    var result = await mediator.Send(
      new GetAgentSessionsPageQuery(limit, afterStartedAtUtc, afterId, includeSessionId, phaseFilter),
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
