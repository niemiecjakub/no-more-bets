using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Infrastructure.AI.Common;

namespace NoMoreBets.Infrastructure.AI.Phases.InternetResearch;

public sealed class InternetResearchPhaseRunner(AgentPhaseExecutor executor)
{
  public async Task<IReadOnlyList<IMessage>> RunAsync(CancellationToken cancellationToken = default)
  {
    var definition = InternetResearchPhaseDefinition.Create();
    var result = await executor.ExecuteAsync(definition, cancellationToken).ConfigureAwait(false);
    return result.Messages;
  }
}
