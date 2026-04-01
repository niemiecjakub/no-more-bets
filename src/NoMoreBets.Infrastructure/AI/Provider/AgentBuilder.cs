using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.OpenAI;
using NoMoreBets.Application.Search;
using NoMoreBets.Infrastructure.AI.Plugins;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

namespace NoMoreBets.Infrastructure.AI.Provider;

public sealed class AgentBuilder
{
  private readonly ContextBuilder _contextBuilder;
  private readonly IConfiguration _configuration;

  public AgentBuilder(
    ContextBuilder contextBuilder,
    IConfiguration configuration,

    IMediator mediator,
    ILogger<AgentBuilder> logger)
  {
    _contextBuilder = contextBuilder;
    _configuration = configuration;
  }

  public AgentConfig Build()
  {
    string modelId = _configuration["OpenAI:ModelId"] ?? throw new InvalidOperationException("OpenAI ModelId is missing.");
    string apiKey = _configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI ApiKey is missing.");

    var credential = new ApiKeyCredential(apiKey);
    var openAiClient = new OpenAIClient(credential);
    ResponsesClient responsesClient = openAiClient.GetResponsesClient();

    OpenAIResponseAgentThread thread = new OpenAIResponseAgentThread(responsesClient);

    OpenAIResponseAgentInvokeOptions options = new OpenAIResponseAgentInvokeOptions()
    {
      ResponseCreationOptions = new CreateResponseOptions()
      {
        TruncationMode = ResponseTruncationMode.Auto,
        ToolChoice = ResponseToolChoice.CreateAutoChoice(),
        ParallelToolCallsEnabled = true,
      },
    };

    OpenAIResponseAgent agent = new OpenAIResponseAgent(responsesClient, modelId)
    {
      Instructions = _contextBuilder.Instructions,
      StoreEnabled = true
    };

    return new(agent, thread, options);
  }
}

public record AgentConfig(OpenAIResponseAgent Agent, OpenAIResponseAgentThread Thread, OpenAIResponseAgentInvokeOptions Options);