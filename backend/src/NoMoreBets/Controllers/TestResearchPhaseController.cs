using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Infrastructure.AI.Phases.Research;

namespace NoMoreBets.Controllers;

/// <summary>Temporary endpoint for manually triggering the research phase. Remove before production.</summary>
[ApiController]
[Route("api/test")]
public class TestResearchPhaseController(
  IWebHostEnvironment environment,
  IUnitOfWork unitOfWork,
  ResearchPhaseRunner researchPhaseRunner) : ControllerBase
{
  [HttpPost("matches/{matchId:int}/research-phase")]
  public async Task<ActionResult<TestResearchPhaseResult>> RunResearchPhase(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    if (!environment.IsDevelopment())
      return NotFound();

    var match = await unitOfWork.Matches
      .GetMatchByIdAsync(matchId, cancellationToken)
      .ConfigureAwait(false);

    if (match is null)
      return NotFound();

    var messages = await researchPhaseRunner
      .RunAsync(match, cancellationToken)
      .ConfigureAwait(false);

    return Ok(new TestResearchPhaseResult(matchId, messages.Count, MapMessages(messages)));
  }

  private static IReadOnlyList<TestResearchPhaseMessageDto> MapMessages(IReadOnlyList<IMessage> messages) =>
    messages.Select(message => message switch
    {
      Message textMessage => new TestResearchPhaseMessageDto("message", textMessage.Text, null, null),
      ReasoningMessage reasoningMessage => new TestResearchPhaseMessageDto("reasoning", reasoningMessage.Text, null, null),
      FunctionMessage functionMessage => new TestResearchPhaseMessageDto(
        "function",
        null,
        functionMessage.Name,
        functionMessage.Arguments?
          .Select(argument => new TestResearchPhaseFunctionArgumentDto(argument.Name, argument.Value))
          .ToList()),
      _ => new TestResearchPhaseMessageDto("unknown", null, null, null),
    }).ToList();
}

public sealed record TestResearchPhaseResult(
  int MatchId,
  int MessageCount,
  IReadOnlyList<TestResearchPhaseMessageDto> Messages);

public sealed record TestResearchPhaseMessageDto(
  string Kind,
  string? Text,
  string? FunctionName,
  IReadOnlyList<TestResearchPhaseFunctionArgumentDto>? Arguments);

public sealed record TestResearchPhaseFunctionArgumentDto(string Name, string? Value);
