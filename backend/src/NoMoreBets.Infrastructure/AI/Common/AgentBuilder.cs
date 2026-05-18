using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Agents.OpenAI;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

namespace NoMoreBets.Infrastructure.AI.Common;

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
    var responsesClient = CreateResponsesClient();
    var thread = CreateThread(responsesClient, _threadProvider.ThreadId);
    var options = CreateInvokeOptions();
    var agent = CreateAgent(responsesClient);
    return new(agent, thread, options);
  }

  public AgentConfig BuildForScheduledJob()
  {
    var responsesClient = CreateResponsesClient();
    var thread = CreateThread(responsesClient);
    var options = CreateInvokeOptions(enableReasoningEffort: true);
    var agent = CreateAgent(responsesClient);
    return new(agent, thread, options);
  }

  private ResponsesClient CreateResponsesClient()
  {
    var credential = new ApiKeyCredential(_openAi.ApiKey);
    var openAiClient = new OpenAIClient(credential);
    return openAiClient.GetResponsesClient();
  }

  private static OpenAIResponseAgentThread CreateThread(ResponsesClient responsesClient, string? threadId = null)
  {
    return string.IsNullOrEmpty(threadId)
      ? new OpenAIResponseAgentThread(responsesClient)
      : new OpenAIResponseAgentThread(responsesClient, threadId);
  }

  private static OpenAIResponseAgentInvokeOptions CreateInvokeOptions(bool enableReasoningEffort = false)
  {
    return new OpenAIResponseAgentInvokeOptions()
    {
      ResponseCreationOptions = new CreateResponseOptions()
      {
        TruncationMode = ResponseTruncationMode.Auto,
        ToolChoice = ResponseToolChoice.CreateAutoChoice(),
        ParallelToolCallsEnabled = false,
        ReasoningOptions = enableReasoningEffort
          ? new ResponseReasoningOptions()
          {
            ReasoningEffortLevel = ResponseReasoningEffortLevel.Medium,
            ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Auto
          }
          : null,
      },
    };
  }

  private OpenAIResponseAgent CreateAgent(ResponsesClient responsesClient)
  {
    return new OpenAIResponseAgent(responsesClient, _openAi.ModelId)
    {
      Instructions = _contextBuilder.Instructions,
      StoreEnabled = true,
    };
  }
}

public record AgentConfig(OpenAIResponseAgent Agent, OpenAIResponseAgentThread Thread, OpenAIResponseAgentInvokeOptions Options);
