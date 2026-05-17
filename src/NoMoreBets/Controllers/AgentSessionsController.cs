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
  private sealed record AgentSessionRow(int Id, AgentSessionPhase Phase, DateTime StartedAt, int MessageCount);

  [HttpGet("agent-sessions")]
  public async Task<ActionResult<AgentSessionsPageDto>> GetAgentSessions(
    [FromQuery] int limit = 25,
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

    var isFirstPage = afterStartedAt is null && afterId is null;
    if (!isFirstPage)
      includeSessionId = null;

    var query = db.AgentSession.AsNoTracking();
    if (afterStartedAt is not null && afterId is not null)
    {
      var cursorStartedAt = DateTimeQueryExtensions.ToUtc(afterStartedAt.Value);
      var cursorId = afterId.Value;
      query = query.Where(s =>
        s.StartedAt < cursorStartedAt
        || (s.StartedAt == cursorStartedAt && s.Id < cursorId));
    }

    var rows = await query
      .OrderByDescending(s => s.StartedAt)
      .ThenByDescending(s => s.Id)
      .Take(limit + 1)
      .Select(s => new AgentSessionRow(
        s.Id,
        s.Phase,
        s.StartedAt,
        s.Messages.Count(m => m.Kind != AgentSessionMessageKind.FunctionCall)))
      .ToListAsync(cancellationToken);

    var hasMore = rows.Count > limit;
    if (hasMore)
      rows.RemoveAt(rows.Count - 1);

    if (isFirstPage && includeSessionId is > 0 && rows.All(r => r.Id != includeSessionId.Value))
    {
      var included = await db.AgentSession
        .AsNoTracking()
        .Where(s => s.Id == includeSessionId.Value)
        .Select(s => new AgentSessionRow(
          s.Id,
          s.Phase,
          s.StartedAt,
          s.Messages.Count(m => m.Kind != AgentSessionMessageKind.FunctionCall)))
        .SingleOrDefaultAsync(cancellationToken);

      if (included is not null)
        rows.Add(included);
    }

    rows = rows
      .OrderByDescending(r => r.StartedAt)
      .ThenByDescending(r => r.Id)
      .ToList();

    var items = await MapSessionRowsAsync(rows, cancellationToken);

    DateTime? nextCursorStartedAt = null;
    int? nextCursorId = null;
    if (hasMore && items.Count > 0)
    {
      var lastItem = items[^1];
      nextCursorStartedAt = lastItem.StartedAt;
      nextCursorId = lastItem.Id;
    }

    return Ok(new AgentSessionsPageDto(items, hasMore, nextCursorStartedAt, nextCursorId));
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

  private async Task<IReadOnlyList<AgentSessionListItemDto>> MapSessionRowsAsync(
    IReadOnlyList<AgentSessionRow> rows,
    CancellationToken cancellationToken)
  {
    var matchIdBySessionId = new Dictionary<int, int>();
    if (rows.Count > 0)
    {
      var sessionIds = rows.Select(r => r.Id).ToList();
      var pairs = await db.MatchAnalysis
        .AsNoTracking()
        .Where(a => a.AgentSessionId != null && sessionIds.Contains(a.AgentSessionId.Value))
        .Select(a => new { SessionId = a.AgentSessionId!.Value, a.MatchId })
        .ToListAsync(cancellationToken);

      foreach (var group in pairs.GroupBy(p => p.SessionId))
        matchIdBySessionId[group.Key] = group.First().MatchId;
    }

    return rows
      .Select(r => new AgentSessionListItemDto(
        r.Id,
        (int)r.Phase,
        r.Phase.ToString(),
        r.StartedAt,
        r.MessageCount,
        matchIdBySessionId.TryGetValue(r.Id, out var matchId) ? matchId : null))
      .ToList();
  }
}

public record AgentSessionsPageDto(
  IReadOnlyList<AgentSessionListItemDto> Items,
  bool HasMore,
  DateTime? NextCursorStartedAt,
  int? NextCursorId);
