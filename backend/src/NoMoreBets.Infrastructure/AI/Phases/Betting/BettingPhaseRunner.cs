using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using NoMoreBets.Infrastructure.XApi;

namespace NoMoreBets.Infrastructure.AI.Phases.Betting;

public sealed class BettingPhaseRunner(
  AgentBuilder agentBuilder,
  AgentRunMessageCollector messageCollector,
  IUnitOfWork unitOfWork,
  AgentSessionContext agentSessionContext,
  IServiceProvider serviceProvider,
  IOptions<XApiOptions> xApiOptions,
  ILogger<BettingPhaseRunner> logger)
{
  public async Task<IReadOnlyList<IMessage>> RunAsync(CancellationToken cancellationToken = default)
  {
    var definition = BettingPhaseDefinition.Create(xApiOptions.Value.IsOAuthConfigured);
    if (definition.Steps.Count == 0)
    {
      throw new InvalidOperationException($"Agent phase {definition.Phase} has no steps configured.");
    }

    var phaseName = definition.Phase.ToString();
    logger.LogInformation("Betting agent phase {Phase} starting", phaseName);

    var startedAt = DateTime.UtcNow;
    var sessionId = await unitOfWork.AgentSessions
      .CreateSessionAsync(definition.Phase, startedAt, cancellationToken)
      .ConfigureAwait(false);
    agentSessionContext.SessionId = sessionId;

    var messages = new List<IMessage>();
    AgentSession? agentSession = null;
    try
    {
      foreach (var step in definition.Steps)
      {
        var tools = step.Implementation.GetTools(serviceProvider);
        var contextProviders = step.Implementation.GetAIContextProviders(serviceProvider);
        var prompt = step.Implementation.BuildPrompt();
        var config = await agentBuilder
          .BuildForScheduledJobAsync(contextProviders, agentSession, cancellationToken)
          .ConfigureAwait(false);
        agentSession ??= config.Session;
        var runOptions = AgentRunOptionsFactory.WithTools(config.DefaultRunOptions, tools);
        await config.Agent
          .RunAsync([new ChatMessage(ChatRole.User, prompt)], config.Session, runOptions, cancellationToken)
          .ConfigureAwait(false);
        var stepMessages = messageCollector.TakeMessages();

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
      "Betting agent phase {Phase} completed with {MessageCount} assistant message(s)",
      phaseName,
      messages.Count);

    return messages;
  }
}
