using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.OpenAI;
using OpenAI.Assistants;
using System.ClientModel;

namespace NoMoreBets.Infrastructure.AI.Agent;

public sealed class AgentBuilder
{
  private readonly Kernel _kernel;
  private readonly ContextBuilder _contextBuilder;
  private readonly IConfiguration _configuration;
  private readonly ILogger<AgentBuilder> _logger;

  public AgentBuilder(
    Kernel kernel,
    ContextBuilder contextBuilder,
    IConfiguration configuration,
    ILogger<AgentBuilder> logger)
  {
    _kernel = kernel;
    _contextBuilder = contextBuilder;
    _configuration = configuration;
    _logger = logger;
  }

  /// <remarks>
  /// A shared assistant is global in OpenAI. If <see cref="Kernel"/> plugins differ per request,
  /// tool definitions on the assistant may not match those plugins until the assistant is recreated.
  /// </remarks>
  public async Task<OpenAIAssistantAgent> BuildAsync(CancellationToken cancellationToken = default)
  {
    string modelId = _configuration["OpenAI:ModelId"] ?? throw new InvalidOperationException("OpenAI ModelId is missing.");
    string apiKey = _configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI ApiKey is missing.");

    var credential = new ApiKeyCredential(apiKey);
    var openAiClient = OpenAIAssistantAgent.CreateOpenAIClient(credential, endpoint: null);
    var assistantClient = openAiClient.GetAssistantClient();

    var assistantIdConfig = _configuration["OpenAI:AssistantId"];
    Assistant assistant;
    if (!string.IsNullOrWhiteSpace(assistantIdConfig))
    {
      try
      {
        assistant = await assistantClient.GetAssistantAsync(assistantIdConfig.Trim(), cancellationToken).ConfigureAwait(false);
      }
      catch (ClientResultException ex) when (ex.Status == 404)
      {
        throw new InvalidOperationException(
          $"OpenAI assistant not found for OpenAI:AssistantId={assistantIdConfig.Trim()}. Remove or fix the id.",
          ex);
      }
    }
    else
    {
      assistant = await assistantClient.CreateAssistantAsync(
          modelId,
          name: "NoMoreBets",
          description: string.Empty,
          instructions: _contextBuilder.Instructions,
          enableCodeInterpreter: false,
          codeInterpreterFileIds: null,
          enableFileSearch: false,
          vectorStoreId: null,
          temperature: null,
          topP: null,
          responseFormat: null,
          metadata: null,
          cancellationToken)
        .ConfigureAwait(false);

      _logger.LogInformation(
        "Created OpenAI assistant {AssistantId}. Set OpenAI:AssistantId in configuration to reuse it and avoid duplicate assistants.",
        assistant.Id);
    }

    return new OpenAIAssistantAgent(assistant, assistantClient, _kernel.Plugins);
  }
}
