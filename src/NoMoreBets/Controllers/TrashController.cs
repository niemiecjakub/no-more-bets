using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Simulation.Simulate;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TrashController(IMediator mediator) : ControllerBase
{
  /// <summary>
  /// Runs the Corporate Carl simulation: research, news, analysis, optional bet slip, memory files.
  /// </summary>
  [HttpPost("simulate")]
  public async Task<ActionResult> Simulate(CancellationToken cancellationToken = default)
  {
    await mediator.Send(new SimulateQuery(), cancellationToken).ConfigureAwait(false);
    return NoContent();
  }
}
