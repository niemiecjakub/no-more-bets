using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;

namespace NoMoreBets.Infrastructure.AI.Phases.DailySlip;

public sealed class DailySlipPhaseRunner(
  AgentBuilder agentBuilder,
  AgentRunMessageCollector messageCollector,
  IUnitOfWork unitOfWork,
  AgentSessionContext agentSessionContext,
  IServiceProvider serviceProvider,
  ILogger<DailySlipPhaseRunner> logger)
{
  public async Task<IReadOnlyList<IMessage>> RunAsync(CancellationToken cancellationToken = default)
  {
    var phaseName = DailySlipPhaseDefinition.Phase.ToString();
    logger.LogInformation("Daily slip phase {Phase} starting", phaseName);

    var startedAt = DateTime.UtcNow;
    var sessionId = await unitOfWork.AgentSessions
      .CreateSessionAsync(DailySlipPhaseDefinition.Phase, startedAt, cancellationToken)
      .ConfigureAwait(false);
    agentSessionContext.SessionId = sessionId;

    var messages = new List<IMessage>();
    AgentSession? agentSession = null;
    try
    {
      var executeResult = await AgentPhaseStepExecutor.RunAsync(
        new DailySlipExecuteStep(),
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
      "Daily slip phase {Phase} completed with {MessageCount} assistant message(s)",
      phaseName,
      messages.Count);

    return messages;
  }
}
