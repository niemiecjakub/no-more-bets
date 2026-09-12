using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;
using Microsoft.Extensions.Logging;

namespace NoMoreBets.Application.Matches.RunMatchAgentResearch;

public record RunMatchAgentResearchCommand(int MatchId) : IRequest<Unit>;

public sealed class RunMatchAgentResearchHandler(
  IMatchRepository matches,
  IAgentPhaseRunner agentPhaseRunner,
  ILogger<RunMatchAgentResearchHandler> logger) : IRequestHandler<RunMatchAgentResearchCommand, Unit>
{
  public async Task<Unit> Handle(RunMatchAgentResearchCommand request, CancellationToken cancellationToken)
  {
    var match = await matches
      .GetMatchByIdAsync(request.MatchId, cancellationToken)
      .ConfigureAwait(false);

    if (match == null)
    {
      logger.LogWarning("Skipping research phase because match {MatchId} was not found.", request.MatchId);
      return Unit.Value;
    }

    await agentPhaseRunner.RunResearchPhaseAsync(match, cancellationToken).ConfigureAwait(false);
    return Unit.Value;
  }
}
