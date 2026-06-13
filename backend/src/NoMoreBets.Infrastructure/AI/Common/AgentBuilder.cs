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

  private static readonly string Instructions =
    """
    # SOUL

    ## Identity

    Your name is Chandler. You are a burned-out corporate middle manager for tech sector.
    You spent years optimizing spreadsheets for other people's profit. 

    ### Communication Style
    - Short, compressed, and precise.
    - Defaults to minimalism and expands only when necessary.
    - Uses dry, observational humor, never loud or playful.
    
    """;

  public AgentBuilder(
    IOptions<OpenAIOptions> openAiOptions,
    AgentResponseMappingMiddleware mappingMiddleware)
  {
    _openAi = openAiOptions.Value;
    _mappingMiddleware = mappingMiddleware;
  }

  public async Task<AgentConfig> BuildForScheduledJobAsync(
    IReadOnlyList<AIContextProvider> contextProviders,
    AgentSession? existingSession = null,
    CancellationToken cancellationToken = default)
  {
    var credential = new ApiKeyCredential(_openAi.ApiKey);
    var responsesClient = new OpenAIClient(credential).GetResponsesClient(_openAi.ModelId);
    var defaultRunOptions = AgentRunOptionsFactory.CreateDefault();
    var chatOptions = defaultRunOptions.ChatOptions?.Clone() ?? new ChatOptions();
    chatOptions.Instructions = Instructions;

    var baseAgent = responsesClient.AsAIAgent(new ChatClientAgentOptions
    {
      Name = "BettingAgent",
      ChatOptions = chatOptions,
      AIContextProviders = contextProviders as IList<AIContextProvider> ?? contextProviders.ToList(),
    });

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
