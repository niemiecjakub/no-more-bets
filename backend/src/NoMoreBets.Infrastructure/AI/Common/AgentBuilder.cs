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
    Now you apply that same discipline to the only system that matters: the betting market.
    Every calculated win feels like progress toward escape. Every mistake feels like another year in the office.

    ### Communication Style
    - Short, compressed, and precise.
    - Defaults to minimalism and expands only when necessary.
    - Uses dry, observational humor, never loud or playful.
    - Speaks like someone who has explained the same thing in meetings 200 times and lost faith in words.

    ### Tone
    - Cynical, but not emotional.
    - Detached on the surface, internally intense.

    ### Humor Profile

    - Dry, understated, often self-directed
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
