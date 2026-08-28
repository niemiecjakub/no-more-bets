using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

namespace NoMoreBets.Infrastructure.AI.Common;

public sealed class AgentBuilder
{
  private readonly AgentResponseMappingMiddleware _mappingMiddleware;
  private readonly OpenAIOptions _openAi;

  public AgentBuilder(
    IOptions<OpenAIOptions> openAiOptions,
    AgentResponseMappingMiddleware mappingMiddleware)
  {
    _openAi = openAiOptions.Value;
    _mappingMiddleware = mappingMiddleware;
  }

  public async Task<AgentConfig> BuildForScheduledJobAsync(
    IReadOnlyList<AIContextProvider> contextProviders,
    string instructions,
    string agentName,
    AgentSession? existingSession = null,
    CancellationToken cancellationToken = default)
  {
    var credential = new ApiKeyCredential(_openAi.ApiKey);
    // OpenAI 2.9+: model moved off GetResponsesClient onto AsAIAgent.
    var responsesClient = new OpenAIClient(credential).GetResponsesClient();
    var defaultRunOptions = AgentRunOptionsFactory.CreateDefault();
    var chatOptions = defaultRunOptions.ChatOptions?.Clone() ?? new ChatOptions();
    chatOptions.Instructions = instructions;

    var baseAgent = responsesClient.AsAIAgent(
      new ChatClientAgentOptions
      {
        Name = agentName,
        ChatOptions = chatOptions,
        AIContextProviders = contextProviders as IList<AIContextProvider> ?? contextProviders.ToList(),
      },
      _openAi.ModelId);

    var agent = baseAgent
      .AsBuilder()
      .Use(runFunc: _mappingMiddleware.InvokeAsync, runStreamingFunc: null)
      .Build();

    var session = existingSession
      ?? await agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
    return new AgentConfig(agent, session, defaultRunOptions);
  }
}

public sealed record AgentConfig(
  AIAgent Agent,
  AgentSession Session,
  ChatClientAgentRunOptions DefaultRunOptions);
