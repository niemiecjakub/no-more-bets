using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Domain.AgentSessions;

namespace NoMoreBets.Infrastructure.AI.Common;

public sealed class AgentPhaseExecutor
{
  private readonly AgentBuilder _agentBuilder;
  private readonly AgentSessionContext _agentSessionContext;
  private readonly ILogger<AgentPhaseExecutor> _logger;
  private readonly IPluginFactory _pluginFactory;
  private readonly IUnitOfWork _unitOfWork;

  public AgentPhaseExecutor(
    AgentBuilder agentBuilder,
    IUnitOfWork unitOfWork,
    AgentSessionContext agentSessionContext,
    IPluginFactory pluginFactory,
    ILogger<AgentPhaseExecutor> logger)
  {
    _agentBuilder = agentBuilder;
    _agentSessionContext = agentSessionContext;
    _logger = logger;
    _pluginFactory = pluginFactory;
    _unitOfWork = unitOfWork;
  }

  public async Task<AgentPhaseRunResult> ExecuteAsync(
    IAgentPhaseDefinition definition,
    CancellationToken cancellationToken = default)
  {
    if (definition.Steps.Count == 0)
    {
      throw new InvalidOperationException($"Agent phase {definition.Phase} has no steps configured.");
    }

    var config = await _agentBuilder.BuildForScheduledJobAsync(cancellationToken).ConfigureAwait(false);
    var phaseName = definition.Phase.ToString();
    _logger.LogInformation("Betting agent phase {Phase} starting", phaseName);

    var startedAt = DateTime.UtcNow;
    var sessionId = await _unitOfWork.AgentSessions
      .CreateSessionAsync(definition.Phase, startedAt, cancellationToken)
      .ConfigureAwait(false);
    _agentSessionContext.SessionId = sessionId;

    var persistedSessionId = (int?)sessionId;
    var messages = new List<IMessage>();
    try
    {
      foreach (var step in definition.Steps)
      {
        var tools = step.Implementation.GetTools(_pluginFactory);
        var prompt = step.Implementation.BuildPrompt();
        var stepMessages = await AgentPhaseTranscriptCollector
          .CollectAsync(config, prompt, tools, cancellationToken)
          .ConfigureAwait(false);

        if (step.PersistTranscript)
        {
          messages.AddRange(stepMessages);
        }
      }
    }
    finally
    {
      try
      {
        if (messages.Count == 0)
        {
          await _unitOfWork.AgentSessions
            .DeleteSessionAsync(sessionId, cancellationToken)
            .ConfigureAwait(false);
          persistedSessionId = null;
        }
        else
        {
          var rows = AgentSessionTranscriptMapper.ToEntities(messages);
          await _unitOfWork.AgentSessions
            .AddMessagesAsync(sessionId, rows, cancellationToken)
            .ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        if (messages.Count == 0)
        {
          _logger.LogError(ex, "Failed to delete empty agent session {SessionId}", sessionId);
        }
        else
        {
          _logger.LogError(ex, "Failed to persist agent session {SessionId} transcript", sessionId);
        }
      }

      _agentSessionContext.SessionId = null;
    }

    _logger.LogInformation(
      "Betting agent phase {Phase} completed with {MessageCount} assistant message(s)",
      phaseName,
      messages.Count);

    return new AgentPhaseRunResult(messages, persistedSessionId);
  }
}
