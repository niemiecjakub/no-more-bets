using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Matches.RunMatchAgentResearch;

public record RunMatchAgentResearchCommand(int MatchId) : IRequest<Unit>;

public sealed class RunMatchAgentResearchHandler(
  IUnitOfWork unitOfWork,
  IAgentPhaseRunner agentPhaseRunner,
  ILogger<RunMatchAgentResearchHandler> logger) : IRequestHandler<RunMatchAgentResearchCommand, Unit>
{
  public async Task<Unit> Handle(RunMatchAgentResearchCommand request, CancellationToken cancellationToken)
  {
    var match = await unitOfWork.Matches
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
