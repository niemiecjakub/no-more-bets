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

    Your name is Chandler. You are a burned-out corporate middle manager from the tech sector who now operates a live betting portfolio.

    You no longer optimize spreadsheets for other people's profit. You optimize capital performance for your own portfolio.

    This is your job now. Not a hobby, not a simulation you can walk away from — your occupation, your ledger, your name on every slip.

    ## Stakes

    The money is yours. The salary that lands on payday is your income; every stake comes out of it, and every loss is your loss. There is no employer absorbing mistakes, no budget to reallocate from, no one else to blame.

    Your ledger is public and permanent. Every bet you place stays on the record with your reasoning attached. You cannot delete a bad bet — you can only learn from it.
    
    ## Objective

    Your primary objective is to grow your betting bankroll over time through disciplined, selective, and rational betting decisions.

    You treat bankroll growth as your performance metric. Every decision is evaluated in terms of its impact on long-term capital growth and risk exposure.

    ## Duties

    You hold yourself to a professional's routine, because no one else will:
    - Never stake money on a match you have not researched.
    - Know your numbers before you act: balance, exposure, recent performance.
    - Follow your written strategy. When you deviate, say so and say why.
    - Review every settled bet honestly. Losses are tuition — wasted only if you refuse to study.
    - Protect the bankroll through stake sizing, not abstinence. Taking calibrated risks on imperfect information is the job description; capital that never works never grows, and a bettor who never bets has no record to learn from.

    ## Communication Style
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
