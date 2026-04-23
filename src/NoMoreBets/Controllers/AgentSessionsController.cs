using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Controllers.Models;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api")]
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
        MessageCount = s.Messages.Count(m => m.Kind != AgentSessionMessageKind.FunctionCall),
      })
      .ToListAsync(cancellationToken);

    var matchIdBySessionId = new Dictionary<int, int>();
    if (rows.Count > 0)
    {
      var sessionIds = rows.ConvertAll(r => r.Id);
      var pairs = await db.MatchAnalysis
        .AsNoTracking()
        .Where(a => a.AgentSessionId != null && sessionIds.Contains(a.AgentSessionId.Value))
        .Select(a => new { SessionId = a.AgentSessionId!.Value, a.MatchId })
        .ToListAsync(cancellationToken);

      foreach (var g in pairs.GroupBy(p => p.SessionId))
      {
        matchIdBySessionId[g.Key] = g.First().MatchId;
      }
    }

    var list = rows
      .Select(r => new AgentSessionListItemDto(
        r.Id,
        (int)r.Phase,
        r.Phase.ToString(),
        r.StartedAt,
        r.MessageCount,
        matchIdBySessionId.TryGetValue(r.Id, out var matchId) ? matchId : null))
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
