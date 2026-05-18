using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Infrastructure.AI.Common;

namespace NoMoreBets.Infrastructure.AI.Phases.InternetResearch;

public sealed class InternetResearchPhaseRunner(
  AgentPhaseExecutor executor,
  InternetResearchPhase phase)
{
  public async Task<IReadOnlyList<IMessage>> RunAsync(CancellationToken cancellationToken = default)
  {
    var result = await executor.ExecuteAsync(phase, cancellationToken).ConfigureAwait(false);
    return result.Messages;
  }
}
