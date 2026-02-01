using System.Linq;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Common;
using NoMoreBets.Features.Rotowire.GetRotowireLineups;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScraperController(IMediator mediator) : ControllerBase
{
  [HttpGet]
  public ActionResult<string> Health()
  {
    return Ok("Ok");
  }

  /// <summary>
  /// Gets soccer lineups from RotoWire (games with team lineups, injuries).
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of game lineups.</returns>
  [HttpGet("rotowire/lineups")]
  public async Task<ActionResult<IReadOnlyList<GameLineupDto>>> GetRotowireLineups(CancellationToken cancellationToken)
  {
    var lineups = await mediator.Send(new GetRotowireLineupsQuery(), cancellationToken);
    return Ok(lineups.Select(GameLineupDto.From).ToList());
  }
}
