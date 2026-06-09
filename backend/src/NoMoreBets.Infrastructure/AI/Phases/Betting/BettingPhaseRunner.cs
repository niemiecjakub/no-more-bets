using Microsoft.Extensions.Options;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.XApi;

namespace NoMoreBets.Infrastructure.AI.Phases.Betting;

public sealed class BettingPhaseRunner(
  AgentPhaseExecutor executor,
  IOptions<XApiOptions> xApiOptions)
{
  public async Task<IReadOnlyList<IMessage>> RunAsync(CancellationToken cancellationToken = default)
  {
    var definition = BettingPhaseDefinition.Create(xApiOptions.Value.IsOAuthConfigured);

    var result = await executor.ExecuteAsync(definition, cancellationToken).ConfigureAwait(false);
    return result.Messages;
  }
}
