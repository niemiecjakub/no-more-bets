using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Controllers.Models;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/database")]
public class AgentSessionsController(AppDbContext db) : ControllerBase
{
  [HttpGet("agent-sessions")]
  public async Task<ActionResult<IReadOnlyList<AgentSessionListItemDto>>> GetAgentSessions(
    CancellationToken cancellationToken = default)
  {
    var rows = await db.AgentSession
      .AsNoTracking()
      .OrderByDescending(s => s.StartedAt)
      .Select(s => new
      {
        s.Id,
        s.Phase,
        s.StartedAt,
        MessageCount = s.Messages.Count,
      })
      .ToListAsync(cancellationToken);

    var list = rows
      .Select(r => new AgentSessionListItemDto(
        r.Id,
        (int)r.Phase,
        r.Phase.ToString(),
        r.StartedAt,
        r.MessageCount))
      .ToList();

    return Ok(list);
  }

  [HttpGet("agent-sessions/{sessionId:int}/messages")]
  public async Task<ActionResult<IReadOnlyList<AgentSessionMessageDto>>> GetAgentSessionMessages(
    int sessionId,
    CancellationToken cancellationToken = default)
  {
    var exists = await db.AgentSession
      .AsNoTracking()
      .AnyAsync(s => s.Id == sessionId, cancellationToken);

    if (!exists)
      return NotFound();

    var messages = await db.AgentSessionMessage
      .AsNoTracking()
      .Where(m => m.SessionId == sessionId)
      .OrderBy(m => m.Ordinal)
      .Select(m => new AgentSessionMessageDto(m.Id, m.SessionId, m.Ordinal, (int)m.Kind, m.Text))
      .ToListAsync(cancellationToken);

    return Ok(messages);
  }
}
