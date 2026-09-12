using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using AgentSessionPhase = NoMoreBets.Domain.AgentSessions.AgentSessionPhase;

namespace NoMoreBets.Infrastructure.AI.Phases.Reflection;

public sealed class ReflectionPhaseRunner(
  AgentBuilder agentBuilder,
  AgentRunMessageCollector messageCollector,
  IBettingRepository betting,
  IAgentSessionRepository agentSessions,
  AgentSessionContext agentSessionContext,
  IServiceProvider serviceProvider,
  ILogger<ReflectionPhaseRunner> logger)
{
  public async Task<IReadOnlyList<IMessage>> RunAsync(CancellationToken cancellationToken = default)
  {
    var slips = await betting
      .GetNonPendingBetSlipsAwaitingReflectionAsync(cancellationToken)
      .ConfigureAwait(false);
    if (slips.Count == 0)
    {
      logger.LogInformation(
        "Skipping reflection agent phase: no settled bet slips awaiting reflection (non-pending with no reflection session).");
      return Array.Empty<IMessage>();
    }

    var reflectionBetSlipIds = slips.Select(s => s.Id).ToList();
    var result = await AgentPhaseSessionRunner.RunAsync(
      AgentSessionPhase.Reflection,
      "Betting agent phase",
      agentBuilder,
      messageCollector,
      agentSessions,
      agentSessionContext,
      serviceProvider,
      logger,
      async (runStep, _, ct) =>
      {
        await runStep(new ReflectionExecuteStep(), persistTranscript: true, null, null, ct)
          .ConfigureAwait(false);
      },
      cancellationToken).ConfigureAwait(false);

    if (result.TranscriptPersisted)
    {
      await betting
        .MarkBetSlipsAgentSessionReflectedAsync(result.SessionId, reflectionBetSlipIds, cancellationToken)
        .ConfigureAwait(false);
    }

    return result.Messages;
  }
}
