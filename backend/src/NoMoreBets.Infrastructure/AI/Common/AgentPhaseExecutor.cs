using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
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
  private readonly IServiceProvider _serviceProvider;
  private readonly IUnitOfWork _unitOfWork;

  public AgentPhaseExecutor(
    AgentBuilder agentBuilder,
    IUnitOfWork unitOfWork,
    AgentSessionContext agentSessionContext,
    IServiceProvider serviceProvider,
    ILogger<AgentPhaseExecutor> logger)
  {
    _agentBuilder = agentBuilder;
    _agentSessionContext = agentSessionContext;
    _logger = logger;
    _serviceProvider = serviceProvider;
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

    var phaseName = definition.Phase.ToString();
    _logger.LogInformation("Betting agent phase {Phase} starting", phaseName);

    var startedAt = DateTime.UtcNow;
    var sessionId = await _unitOfWork.AgentSessions
      .CreateSessionAsync(definition.Phase, startedAt, cancellationToken)
      .ConfigureAwait(false);
    _agentSessionContext.SessionId = sessionId;

    var persistedSessionId = (int?)sessionId;
    var messages = new List<IMessage>();
    Microsoft.Agents.AI.AgentSession? agentSession = null;
    try
    {
      foreach (var step in definition.Steps)
      {
        var tools = step.Implementation.GetTools(_serviceProvider);
        var contextProviders = step.Implementation.GetAIContextProviders(_serviceProvider);
        var prompt = step.Implementation.BuildPrompt();
        var config = await _agentBuilder
          .BuildForScheduledJobAsync(contextProviders, agentSession, cancellationToken)
          .ConfigureAwait(false);
        agentSession ??= config.Session;
        var stepMessages = await CollectAsync(config, prompt, tools, cancellationToken)
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

  private static async Task<List<IMessage>> CollectAsync(
    AgentConfig config,
    string prompt,
    IReadOnlyList<AITool> tools,
    CancellationToken cancellationToken)
  {
    var runOptions = AgentRunOptionsFactory.WithTools(config.DefaultRunOptions, tools);
    var response = await config.Agent
      .RunAsync(prompt, config.Session, runOptions, cancellationToken)
      .ConfigureAwait(false);

    return MapResponse(response);
  }

  private static List<IMessage> MapResponse(AgentResponse response)
  {
    var messages = new List<IMessage>();

    foreach (var chatMessage in response.Messages)
    {
      foreach (var item in chatMessage.Contents)
      {
        switch (item)
        {
          case TextReasoningContent reasoning when !string.IsNullOrEmpty(reasoning.Text):
            messages.Add(new ReasoningMessage(reasoning.Text));
            break;

          case FunctionCallContent functionCall:
            var arguments = functionCall.Arguments?
              .Select(a => new FunctionArgument(a.Key, a.Value?.ToString()))
              .ToList();
            messages.Add(new FunctionMessage(functionCall.Name, arguments));
            break;

          case TextContent text when !string.IsNullOrEmpty(text.Text):
            messages.Add(new Message(text.Text));
            break;
        }
      }
    }

    if (messages.Count == 0 && !string.IsNullOrEmpty(response.Text))
    {
      messages.Add(new Message(response.Text));
    }

    return messages;
  }
}
