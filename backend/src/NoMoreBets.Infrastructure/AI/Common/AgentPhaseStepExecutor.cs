using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;

namespace NoMoreBets.Infrastructure.AI.Common;

internal sealed record AgentPhaseStepResult(AgentSession Session, AgentResponse Response);

internal static class AgentPhaseStepExecutor
{
  public static async Task<AgentPhaseStepResult> RunAsync(
    IAgentPhaseStep step,
    bool persistTranscript,
    Type? responseFormatType,
    AgentBuilder agentBuilder,
    AgentRunMessageCollector messageCollector,
    IServiceProvider serviceProvider,
    AgentSession? agentSession,
    List<IMessage> messages,
    CancellationToken cancellationToken)
  {
    var tools = step.GetTools(serviceProvider);
    var contextProviders = step.GetAIContextProviders(serviceProvider);
    var prompt = step.BuildPrompt();
    var config = await agentBuilder
      .BuildForScheduledJobAsync(contextProviders, agentSession, cancellationToken)
      .ConfigureAwait(false);
    agentSession ??= config.Session;
    var runOptions = AgentRunOptionsFactory.WithTools(config.DefaultRunOptions, tools);
    runOptions.ResponseFormat = responseFormatType is not null
      ? ChatResponseFormat.ForJsonSchema(responseFormatType)
      : ChatResponseFormat.Text;
    var response = await config.Agent
      .RunAsync([new ChatMessage(ChatRole.User, prompt)], config.Session, runOptions, cancellationToken)
      .ConfigureAwait(false);
    var stepMessages = messageCollector.TakeMessages();

    if (persistTranscript)
    {
      messages.AddRange(stepMessages);
    }

    return new AgentPhaseStepResult(agentSession, response);
  }
}
