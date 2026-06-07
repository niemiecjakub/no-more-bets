using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

namespace NoMoreBets.Infrastructure.AI.Common;

public sealed class AgentBuilder
{
  private readonly OpenAIOptions _openAi;

  public AgentBuilder(IOptions<OpenAIOptions> openAiOptions)
  {
    _openAi = openAiOptions.Value;
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
    chatOptions.Instructions = LoadInstructions();

    var agent = responsesClient.AsAIAgent(new ChatClientAgentOptions
    {
      Name = "BettingAgent",
      ChatOptions = chatOptions,
      AIContextProviders = contextProviders as IList<AIContextProvider> ?? contextProviders.ToList(),
    });

    var session = existingSession
      ?? await agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
    return new AgentConfig(agent, session, defaultRunOptions);
  }

  private static string LoadInstructions()
  {
    var workspace = Path.Combine(AppContext.BaseDirectory, "AI", "Common");
    var path = Path.Combine(workspace, "SOUL.md");
    return File.Exists(path)
      ? $"# SOUL\n\n{File.ReadAllText(path)}"
      : string.Empty;
  }
}

public sealed record AgentConfig(
  ChatClientAgent Agent,
  AgentSession Session,
  ChatClientAgentRunOptions DefaultRunOptions);
