using Microsoft.AspNetCore.Mvc;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
  [HttpGet("time")]
  public ActionResult<ServerTimeDto> GetServerTime()
  {
    var utcNow = DateTime.UtcNow;
    return Ok(new ServerTimeDto(utcNow, new DateTimeOffset(utcNow).ToUnixTimeSeconds()));
  }
}

public sealed record ServerTimeDto(DateTime UtcNow, long UnixSeconds);
