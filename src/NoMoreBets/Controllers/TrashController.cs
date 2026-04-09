using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Provider;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TrashController(
  Runner runner,
  IAgentPhaseRunner agentPhaseRunner,
  IUnitOfWork unitOfWork) : ControllerBase
{
  /// <summary>
  /// Sends a user message to the OpenAI assistant runner and returns the assistant reply text.
  /// Request body: JSON string, e.g. <c>"Hello"</c>.
  /// </summary>
  [HttpPost("agent/message")]
  public async Task<ActionResult<List<string>>> PostAgentMessage(
    [FromBody] string message,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(message))
    {
      return BadRequest("Message is required.");
    }

    var messages = await runner.Chat(message.Trim(), cancellationToken).ConfigureAwait(false);

    return Ok(messages.Select(m => m.Content).ToArray());
  }

  [HttpPost("agent/run-research-phase")]
  public async Task<ActionResult<IReadOnlyList<string>>> RunResearchPhase(
    [FromQuery] int matchId,
    CancellationToken cancellationToken = default)
  {
    var match = await unitOfWork.Matches.GetMatchByIdAsync(matchId, cancellationToken).ConfigureAwait(false);
    if (match is null)
    {
      return NotFound($"Match with id {matchId} was not found.");
    }

    var messages = await agentPhaseRunner.RunResearchPhaseAsync(match, cancellationToken).ConfigureAwait(false);
    return Ok(messages);
  }

  [HttpPost("agent/run-reflection-phase")]
  public async Task<ActionResult<IReadOnlyList<string>>> RunReflectionPhase(CancellationToken cancellationToken = default)
  {
    var messages = await agentPhaseRunner.RunReflectionPhaseAsync(cancellationToken).ConfigureAwait(false);
    return Ok(messages);
  }

  [HttpPost("agent/run-betting-execution-phase")]
  public async Task<ActionResult<IReadOnlyList<string>>> RunBettingExecutionPhase(CancellationToken cancellationToken = default)
  {
    var messages = await agentPhaseRunner.RunBettingExecutionPhaseAsync(cancellationToken).ConfigureAwait(false);
    return Ok(messages);
  }
}
