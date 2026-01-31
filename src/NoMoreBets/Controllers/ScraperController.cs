using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Domain.Entities.Rotowire;
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
  /// Gets soccer lineups from RotoWire (games with team lineups, injuries, odds, weather).
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of game lineups.</returns>
  [HttpGet("rotowire/lineups")]
  public async Task<ActionResult<IReadOnlyList<GameLineup>>> GetRotowireLineups(CancellationToken cancellationToken)
  {
    return Ok(await mediator.Send(new GetRotowireLineupsQuery(), cancellationToken));
  }
}
