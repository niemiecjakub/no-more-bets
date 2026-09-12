using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using AgentSessionPhase = NoMoreBets.Domain.AgentSessions.AgentSessionPhase;
using IAgentSessionRepository = NoMoreBets.Domain.AgentSessions.IAgentSessionRepository;

namespace NoMoreBets.Infrastructure.AI.Common;

internal sealed record AgentPhaseSessionResult(
  int SessionId,
  IReadOnlyList<IMessage> Messages,
  bool TranscriptPersisted);

internal static class AgentPhaseSessionRunner
{
  public delegate Task<AgentPhaseStepResult> RunStep(
    IAgentPhaseStep step,
    bool persistTranscript,
    Type? responseFormatType,
    AgentSession? agentSession,
    CancellationToken cancellationToken);

  public delegate Task RunBody(
    RunStep runStep,
    List<IMessage> messages,
    CancellationToken cancellationToken);

  public static async Task<AgentPhaseSessionResult> RunAsync(
    AgentSessionPhase phase,
    string logLabel,
    AgentBuilder agentBuilder,
    AgentRunMessageCollector messageCollector,
    IAgentSessionRepository agentSessions,
    AgentSessionContext agentSessionContext,
    IServiceProvider serviceProvider,
    ILogger logger,
    RunBody body,
    CancellationToken cancellationToken)
  {
    var phaseName = phase.ToString();
    logger.LogInformation("{LogLabel} {Phase} starting", logLabel, phaseName);

    var startedAt = DateTime.UtcNow;
    var sessionId = await agentSessions
      .CreateSessionAsync(phase, startedAt, cancellationToken)
      .ConfigureAwait(false);
    agentSessionContext.SessionId = sessionId;

    var messages = new List<IMessage>();
    var transcriptPersisted = false;
    try
    {
      await body(
        (step, persistTranscript, responseFormatType, agentSession, ct) =>
          AgentPhaseStepExecutor.RunAsync(
            step,
            persistTranscript,
            responseFormatType,
            agentBuilder,
            messageCollector,
            serviceProvider,
            agentSession,
            messages,
            ct),
        messages,
        cancellationToken).ConfigureAwait(false);
    }
    finally
    {
      try
      {
        if (messages.Count == 0)
        {
          await agentSessions
            .DeleteSessionAsync(sessionId, cancellationToken)
            .ConfigureAwait(false);
        }
        else
        {
          var rows = AgentSessionTranscriptMapper.ToEntities(messages);
          await agentSessions
            .AddMessagesAsync(sessionId, rows, cancellationToken)
            .ConfigureAwait(false);
          transcriptPersisted = true;
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
      "{LogLabel} {Phase} completed with {MessageCount} assistant message(s)",
      logLabel,
      phaseName,
      messages.Count);

    return new AgentPhaseSessionResult(sessionId, messages, transcriptPersisted);
  }
}
