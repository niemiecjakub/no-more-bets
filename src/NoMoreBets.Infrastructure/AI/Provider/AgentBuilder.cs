using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Infrastructure.AI;
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
  private readonly OpenAIOptions _openAi;
  private readonly ThreadProvider _threadProvider;

  public AgentBuilder(
    ContextBuilder contextBuilder,
    IOptions<OpenAIOptions> openAiOptions,
    ThreadProvider threadProvider)
  {
    _contextBuilder = contextBuilder;
    _openAi = openAiOptions.Value;
    _threadProvider = threadProvider;
  }

  public AgentConfig Build()
  {
    var credential = new ApiKeyCredential(_openAi.ApiKey);
    var openAiClient = new OpenAIClient(credential);
    ResponsesClient responsesClient = openAiClient.GetResponsesClient();

    OpenAIResponseAgentThread thread = string.IsNullOrEmpty(_threadProvider.ThreadId)
      ? new OpenAIResponseAgentThread(responsesClient)
      : new OpenAIResponseAgentThread(responsesClient, _threadProvider.ThreadId);

    OpenAIResponseAgentInvokeOptions options = new OpenAIResponseAgentInvokeOptions()
    {
      ResponseCreationOptions = new CreateResponseOptions()
      {
        TruncationMode = ResponseTruncationMode.Auto,
        ToolChoice = ResponseToolChoice.CreateAutoChoice(),
        ParallelToolCallsEnabled = true,
      },
    };

    OpenAIResponseAgent agent = new OpenAIResponseAgent(responsesClient, _openAi.ModelId)
    {
      Instructions = _contextBuilder.Instructions,
      StoreEnabled = true
    };

    return new(agent, thread, options);
  }
}

public record AgentConfig(OpenAIResponseAgent Agent, OpenAIResponseAgentThread Thread, OpenAIResponseAgentInvokeOptions Options);