using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Infrastructure.Mcp;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/mcp")]
public class McpController : ControllerBase
{
  [HttpGet("tools")]
  public ActionResult<IReadOnlyList<McpToolGroupDto>> GetTools()
  {
    return Ok(McpToolCatalog.ListGroups());
  }
}
