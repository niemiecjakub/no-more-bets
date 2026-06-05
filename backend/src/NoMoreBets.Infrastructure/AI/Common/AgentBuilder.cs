using MediatR;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Providers;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

namespace NoMoreBets.Infrastructure.AI.Common;

public sealed class AgentBuilder
{
  private readonly OpenAIOptions _openAi;
  private readonly IMediator _mediator;
  private readonly IUnitOfWork _unitOfWork;

  public AgentBuilder(IOptions<OpenAIOptions> openAiOptions, IMediator mediator, IUnitOfWork unitOfWork)
  {
    _mediator = mediator;
    _unitOfWork = unitOfWork;
    _openAi = openAiOptions.Value;
  }

  public async Task<AgentConfig> BuildForScheduledJobAsync(CancellationToken cancellationToken = default)
  {
    var responsesClient = CreateResponsesClient();
    var agent = responsesClient.AsAIAgent(
      instructions: LoadInstructions(),
      name: "BettingAgent");


    responsesClient.AsAIAgent(new ChatClientAgentOptions()
    {
      AIContextProviders = [
        new BankrollProvider(_mediator),
        new MemoriesProvider(_unitOfWork),
      ]
    });

    var session = await agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
    var defaultRunOptions = AgentRunOptionsFactory.CreateDefault(enableReasoningEffort: true);
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

  private ResponsesClient CreateResponsesClient()
  {
    var credential = new ApiKeyCredential(_openAi.ApiKey);
    var openAiClient = new OpenAIClient(credential);
    return openAiClient.GetResponsesClient(_openAi.ModelId);
  }
}

public sealed record AgentConfig(
  AIAgent Agent,
  AgentSession Session,
  ChatClientAgentRunOptions DefaultRunOptions);
