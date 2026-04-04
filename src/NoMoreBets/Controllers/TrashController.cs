using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Infrastructure.AI.Provider;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TrashController(Runner runner) : ControllerBase
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
}
