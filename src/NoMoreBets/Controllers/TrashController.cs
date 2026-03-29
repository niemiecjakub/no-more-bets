using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Simulation.Simulate;
using NoMoreBets.Infrastructure.AI.Agent;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TrashController(IMediator mediator, Runner runner) : ControllerBase
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

  /// <summary>
  /// Sends a user message to the OpenAI assistant runner and returns the assistant reply text.
  /// Request body: JSON string, e.g. <c>"Hello"</c>.
  /// </summary>
  [HttpPost("agent/message")]
  public async Task<ActionResult<string>> PostAgentMessage(
    [FromBody] string message,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(message))
    {
      return BadRequest("Message is required.");
    }

    var reply = await runner.RunTurnAsync(message.Trim(), cancellationToken).ConfigureAwait(false);
    return Ok(reply.Content ?? string.Empty);
  }
}
