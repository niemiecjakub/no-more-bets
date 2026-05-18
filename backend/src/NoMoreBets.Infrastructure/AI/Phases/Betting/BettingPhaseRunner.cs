using MediatR;
using Microsoft.Extensions.Options;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.XApi;

namespace NoMoreBets.Infrastructure.AI.Phases.Betting;

public sealed class BettingPhaseRunner(
  AgentPhaseExecutor executor,
  IPluginFactory pluginFactory,
  IUnitOfWork unitOfWork,
  IMediator mediator,
  IOptions<XApiOptions> xApiOptions)
{
  public async Task<IReadOnlyList<IMessage>> RunAsync(CancellationToken cancellationToken = default)
  {
    var definition = await BettingPhaseDefinition
      .CreateAsync(
        unitOfWork,
        mediator,
        pluginFactory,
        xApiOptions.Value.IsOAuthConfigured,
        cancellationToken)
      .ConfigureAwait(false);

    var result = await executor.ExecuteAsync(definition, cancellationToken).ConfigureAwait(false);
    return result.Messages;
  }
}
