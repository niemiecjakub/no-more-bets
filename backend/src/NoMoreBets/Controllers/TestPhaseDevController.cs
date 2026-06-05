using Microsoft.AspNetCore.Mvc;
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
  IWebHostEnvironment environment) : ControllerBase
{
  [HttpPost("run")]
  public async Task<ActionResult<AgentPhaseRunResponseDto>> Run(CancellationToken cancellationToken)
  {
    if (!environment.IsDevelopment())
      return NotFound();

    var messages = await testPhaseRunner.RunAsync(cancellationToken).ConfigureAwait(false);

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
