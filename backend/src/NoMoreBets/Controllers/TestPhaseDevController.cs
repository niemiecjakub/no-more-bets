using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Infrastructure.AI.Phases.Test;

namespace NoMoreBets.Controllers;

/// <summary>
/// Development-only endpoint for running the test phase agent directly.
/// </summary>
[ApiController]
[Route("api/dev/test-phase")]
public sealed class TestPhaseDevController(
  TestPhaseRunner testPhaseRunner,
  IUnitOfWork unitOfWork,
  IWebHostEnvironment environment) : ControllerBase
{
  [HttpPost("run")]
  public async Task<ActionResult<AgentPhaseRunResponseDto>> Run(
    [FromQuery] int matchId,
    CancellationToken cancellationToken)
  {
    if (!environment.IsDevelopment())
      return NotFound();

    var match = await unitOfWork.Matches
      .GetMatchByIdAsync(matchId, cancellationToken)
      .ConfigureAwait(false);

    if (match is null)
      return NotFound($"Match {matchId} was not found.");

    var messages = await testPhaseRunner.RunAsync(match, cancellationToken).ConfigureAwait(false);

    return Ok(new AgentPhaseRunResponseDto(
      "Test",
      messages.Count,
      MapMessages(messages)));
  }

  private static IReadOnlyList<AgentPhaseMessageDto> MapMessages(IReadOnlyList<IMessage> messages) =>
    messages
      .Select(message => message switch
      {
        Message m => new AgentPhaseMessageDto("message", m.Text, null, null),
        ReasoningMessage r => new AgentPhaseMessageDto("reasoning", r.Text, null, null),
        FunctionMessage f => new AgentPhaseMessageDto(
          "function",
          null,
          f.Name,
          f.Arguments?
            .Select(a => new AgentPhaseFunctionArgumentDto(a.Name, a.Value))
            .ToList()),
        _ => new AgentPhaseMessageDto("unknown", null, null, null),
      })
      .ToList();
}
