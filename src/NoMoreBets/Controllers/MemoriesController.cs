using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Controllers.Models;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api")]
public class MemoriesController(AppDbContext db) : ControllerBase
{
  [HttpGet("memories")]
  public async Task<ActionResult<MemoriesPageDto>> GetMemories(
    [FromQuery] int limit = 25,
    [FromQuery] DateTime? afterUpdatedAt = null,
    [FromQuery] int? afterId = null,
    CancellationToken cancellationToken = default)
  {
    limit = Math.Clamp(limit, 1, 100);

    if (afterUpdatedAt is null != afterId is null)
    {
      return BadRequest("afterUpdatedAt and afterId must both be provided or omitted.");
    }

    var query = db.Memory
      .AsNoTracking()
      .Where(m => m.DeletedAt == null);

    if (afterUpdatedAt is not null && afterId is not null)
    {
      var cursorUpdatedAt = DateTimeQueryExtensions.ToUtc(afterUpdatedAt.Value);
      var cursorId = afterId.Value;
      query = query.Where(m =>
        m.UpdatedAt < cursorUpdatedAt
        || (m.UpdatedAt == cursorUpdatedAt && m.Id < cursorId));
    }

    var rows = await query
      .OrderByDescending(m => m.UpdatedAt)
      .ThenByDescending(m => m.Id)
      .Take(limit + 1)
      .Select(m => new MemoryListItemDto(
        m.Id,
        m.Name,
        m.Description,
        m.Content,
        m.CreatedAt,
        m.UpdatedAt))
      .ToListAsync(cancellationToken);

    var hasMore = rows.Count > limit;
    if (hasMore)
      rows.RemoveAt(rows.Count - 1);

    DateTime? nextCursorUpdatedAt = null;
    int? nextCursorId = null;
    if (hasMore && rows.Count > 0)
    {
      var lastItem = rows[^1];
      nextCursorUpdatedAt = lastItem.UpdatedAt;
      nextCursorId = lastItem.Id;
    }

    return Ok(new MemoriesPageDto(rows, hasMore, nextCursorUpdatedAt, nextCursorId));
  }
}

public record MemoriesPageDto(
  IReadOnlyList<MemoryListItemDto> Items,
  bool HasMore,
  DateTime? NextCursorUpdatedAt,
  int? NextCursorId);
