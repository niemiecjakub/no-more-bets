using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Domain.AgentSessions;

namespace NoMoreBets.Controllers;

/// <summary>
/// Temporary development-only endpoints for manually triggering agent phases.
/// </summary>
[ApiController]
[Route("api/dev/agent-phases")]
public sealed class AgentPhasesDevController(
  IAgentPhaseRunner agentPhaseRunner,
  IUnitOfWork unitOfWork,
  IWebHostEnvironment environment) : ControllerBase
{
  [HttpPost("run")]
  public async Task<ActionResult<AgentPhaseRunResponseDto>> RunPhase(
    [FromBody] RunAgentPhaseRequest request,
    CancellationToken cancellationToken)
  {
    if (!environment.IsDevelopment())
      return NotFound();

    if (!Enum.IsDefined(typeof(AgentSessionPhase), request.Phase))
      return BadRequest($"Invalid phase value: {request.Phase}.");

    var phase = (AgentSessionPhase)request.Phase;
    IReadOnlyList<IMessage> messages;

    switch (phase)
    {
      case AgentSessionPhase.Research:
        if (request.MatchId is not int matchId)
          return BadRequest("matchId is required for the Research phase.");

        var match = await unitOfWork.Matches
          .GetMatchByIdAsync(matchId, cancellationToken)
          .ConfigureAwait(false);

        if (match is null)
          return NotFound($"Match {matchId} was not found.");

        messages = await agentPhaseRunner
          .RunResearchPhaseAsync(match, cancellationToken)
          .ConfigureAwait(false);
        break;

      case AgentSessionPhase.Betting:
        messages = await agentPhaseRunner
          .RunBettingExecutionPhaseAsync(cancellationToken)
          .ConfigureAwait(false);
        break;

      case AgentSessionPhase.Reflection:
        messages = await agentPhaseRunner
          .RunReflectionPhaseAsync(cancellationToken)
          .ConfigureAwait(false);
        break;

      case AgentSessionPhase.InternetResearch:
        messages = await agentPhaseRunner
          .RunUpcomingMatchesInternetResearchAsync(cancellationToken)
          .ConfigureAwait(false);
        break;

      case AgentSessionPhase.MemoryCleanup:
        messages = await agentPhaseRunner
          .RunMemoryCleanupPhaseAsync(cancellationToken)
          .ConfigureAwait(false);
        break;

      default:
        return BadRequest($"Phase {phase} is not supported.");
    }

    return Ok(new AgentPhaseRunResponseDto(
      phase.ToString(),
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

public sealed record RunAgentPhaseRequest(int Phase, int? MatchId = null);

public sealed record AgentPhaseRunResponseDto(
  string Phase,
  int MessageCount,
  IReadOnlyList<AgentPhaseMessageDto> Messages);

public sealed record AgentPhaseMessageDto(
  string Kind,
  string? Text,
  string? FunctionName,
  IReadOnlyList<AgentPhaseFunctionArgumentDto>? Arguments);

public sealed record AgentPhaseFunctionArgumentDto(string Name, string? Value);
