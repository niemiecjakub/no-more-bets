using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using NoMoreBets.Infrastructure.XApi;
using AgentSessionPhase = NoMoreBets.Domain.AgentSessions.AgentSessionPhase;

namespace NoMoreBets.Infrastructure.AI.Phases.Betting;

public sealed class BettingPhaseRunner(
  AgentBuilder agentBuilder,
  AgentRunMessageCollector messageCollector,
  IAgentSessionRepository agentSessions,
  AgentSessionContext agentSessionContext,
  IServiceProvider serviceProvider,
  IOptions<XApiOptions> xApiOptions,
  ILogger<BettingPhaseRunner> logger)
{
  public async Task<IReadOnlyList<IMessage>> RunAsync(CancellationToken cancellationToken = default)
  {
    var definition = BettingPhaseDefinition.Create(xApiOptions.Value.IsOAuthConfigured);
    var result = await AgentPhaseSessionRunner.RunAsync(
      AgentSessionPhase.Betting,
      "Betting agent phase",
      agentBuilder,
      messageCollector,
      agentSessions,
      agentSessionContext,
      serviceProvider,
      logger,
      async (runStep, _, ct) =>
      {
        var executeResult = await runStep(new BettingExecuteStep(), persistTranscript: true, null, null, ct)
          .ConfigureAwait(false);

        if (definition.IncludeXPostFollowUp)
        {
          await runStep(new XPostFollowUpStep(), persistTranscript: false, null, executeResult.Session, ct)
            .ConfigureAwait(false);
        }
      },
      cancellationToken).ConfigureAwait(false);

    return result.Messages;
  }
}
