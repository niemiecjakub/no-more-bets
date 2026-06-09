using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.AI.Common;

namespace NoMoreBets.Infrastructure.AI.Phases.Research;

public sealed class ResearchPhaseRunner(AgentPhaseExecutor executor)
{
  public async Task<IReadOnlyList<IMessage>> RunAsync(Match match, CancellationToken cancellationToken = default)
  {
    var definition = ResearchPhaseDefinition.ForMatch(match);
    var result = await executor.ExecuteAsync(definition, cancellationToken).ConfigureAwait(false);
    return result.Messages;
  }
}
