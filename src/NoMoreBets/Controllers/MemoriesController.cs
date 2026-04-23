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
  public async Task<ActionResult<IReadOnlyList<MemoryListItemDto>>> GetMemories(
    CancellationToken cancellationToken = default)
  {
    var list = await db.Memory
      .AsNoTracking()
      .Where(m => m.DeletedAt == null)
      .OrderBy(m => m.Name)
      .Select(m => new MemoryListItemDto(
        m.Id,
        m.Name,
        m.Description,
        m.Content,
        m.CreatedAt,
        m.UpdatedAt))
      .ToListAsync(cancellationToken);
    return Ok(list);
  }
}
