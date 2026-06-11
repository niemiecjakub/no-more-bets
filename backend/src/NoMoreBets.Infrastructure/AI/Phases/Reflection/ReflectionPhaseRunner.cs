using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;

namespace NoMoreBets.Infrastructure.AI.Phases.Reflection;

public sealed class ReflectionPhaseRunner(
  AgentBuilder agentBuilder,
  AgentRunMessageCollector messageCollector,
  IUnitOfWork unitOfWork,
  AgentSessionContext agentSessionContext,
  IServiceProvider serviceProvider,
  ILogger<ReflectionPhaseRunner> logger)
{
  public async Task<IReadOnlyList<IMessage>> RunAsync(CancellationToken cancellationToken = default)
  {
    var slips = await unitOfWork.Betting
      .GetNonPendingBetSlipsAwaitingReflectionAsync(cancellationToken)
      .ConfigureAwait(false);
    if (slips.Count == 0)
    {
      logger.LogInformation(
        "Skipping reflection agent phase: no settled bet slips awaiting reflection (non-pending with no reflection session).");
      return Array.Empty<IMessage>();
    }

    var reflectionBetSlipIds = slips.Select(s => s.Id).ToList();
    var phaseName = ReflectionPhaseDefinition.Phase.ToString();
    logger.LogInformation("Betting agent phase {Phase} starting", phaseName);

    var startedAt = DateTime.UtcNow;
    var sessionId = await unitOfWork.AgentSessions
      .CreateSessionAsync(ReflectionPhaseDefinition.Phase, startedAt, cancellationToken)
      .ConfigureAwait(false);
    agentSessionContext.SessionId = sessionId;

    var persistedSessionId = (int?)sessionId;
    var messages = new List<IMessage>();
    AgentSession? agentSession = null;
    try
    {
      var executeResult = await AgentPhaseStepExecutor.RunAsync(
        new ReflectionExecuteStep(),
        persistTranscript: true,
        responseFormatType: null,
        agentBuilder,
        messageCollector,
        serviceProvider,
        agentSession,
        messages,
        cancellationToken).ConfigureAwait(false);
      agentSession = executeResult.Session;
    }
    finally
    {
      try
      {
        if (messages.Count == 0)
        {
          await unitOfWork.AgentSessions
            .DeleteSessionAsync(sessionId, cancellationToken)
            .ConfigureAwait(false);
          persistedSessionId = null;
        }
        else
        {
          var rows = AgentSessionTranscriptMapper.ToEntities(messages);
          await unitOfWork.AgentSessions
            .AddMessagesAsync(sessionId, rows, cancellationToken)
            .ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        if (messages.Count == 0)
        {
          logger.LogError(ex, "Failed to delete empty agent session {SessionId}", sessionId);
        }
        else
        {
          logger.LogError(ex, "Failed to persist agent session {SessionId} transcript", sessionId);
        }
      }

      agentSessionContext.SessionId = null;
    }

    logger.LogInformation(
      "Betting agent phase {Phase} completed with {MessageCount} assistant message(s)",
      phaseName,
      messages.Count);

    if (reflectionBetSlipIds.Count > 0 && persistedSessionId is int reflectionSessionId)
    {
      await unitOfWork.Betting
        .MarkBetSlipsAgentSessionReflectedAsync(reflectionSessionId, reflectionBetSlipIds, cancellationToken)
        .ConfigureAwait(false);
    }

    return messages;
  }
}
