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
    bool loopUntilBackgroundTasksComplete = false,
    CancellationToken cancellationToken = default)
  {
    var baseAgent = CreateBaseAgent(contextProviders, instructions, agentName, tools: null);

    if (loopUntilBackgroundTasksComplete)
    {
      baseAgent = new LoopAgent(
        baseAgent,
        new BackgroundTaskCompletionLoopEvaluator(),
        new LoopAgentOptions { MaxIterations = 4,  });
    }

    var agent = baseAgent
      .AsBuilder()
      .Use(runFunc: _mappingMiddleware.InvokeAsync, runStreamingFunc: null)
      .Build();

    var defaultRunOptions = AgentRunOptionsFactory.CreateDefault();
    var session = existingSession
      ?? await agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
    return new AgentConfig(agent, session, defaultRunOptions);
  }

  public AIAgent CreateChildAgent(
    string name,
    string instructions,
    string description,
    IReadOnlyList<AIContextProvider> contextProviders,
    IReadOnlyList<AITool> tools)
  {
    return CreateBaseAgent(contextProviders, instructions, name, tools, description);
  }

  private AIAgent CreateBaseAgent(
    IReadOnlyList<AIContextProvider> contextProviders,
    string instructions,
    string agentName,
    IReadOnlyList<AITool>? tools,
    string? description = null)
  {
    var credential = new ApiKeyCredential(_openAi.ApiKey);
    var responsesClient = new OpenAIClient(credential).GetResponsesClient();
    var chatOptions = AgentRunOptionsFactory.CreateDefault().ChatOptions?.Clone() ?? new ChatOptions();
    chatOptions.Instructions = instructions;
    if (tools is { Count: > 0 })
    {
      chatOptions.Tools = tools as IList<AITool> ?? tools.ToList();
    }

    return responsesClient.AsAIAgent(
      new ChatClientAgentOptions
      {
        Name = agentName,
        Description = description,
        ChatOptions = chatOptions,
        AIContextProviders = contextProviders as IList<AIContextProvider> ?? contextProviders.ToList(),
      },
      _openAi.ModelId);
  }
}

public sealed record AgentConfig(
  AIAgent Agent,
  AgentSession Session,
  ChatClientAgentRunOptions DefaultRunOptions);
