using MediatR;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Application.Search;
using NoMoreBets.Infrastructure.AI.Providers;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;

namespace NoMoreBets.Infrastructure.AI.Phases.Test;

public sealed class TestPhaseRunner(
  IOptions<OpenAIOptions> openAiOptions,
  IMediator mediator,
  IUnitOfWork unitOfWork,
  ISearchService searchService)
{
  public async Task<IReadOnlyList<IMessage>> RunAsync(CancellationToken cancellationToken = default)
  {
    var openAi = openAiOptions.Value;
    var credential = new ApiKeyCredential(openAi.ApiKey);
    var responsesClient = new OpenAIClient(credential).GetResponsesClient(openAi.ModelId);

    var agent = responsesClient.AsAIAgent(new ChatClientAgentOptions
    {
      Name = "TestAgent",
      ChatOptions = new ChatOptions
      {
        Instructions = LoadInstructions(),
        AllowMultipleToolCalls = true,
        ToolMode = ChatToolMode.Auto,
        Reasoning = new ReasoningOptions
        {
          Effort = ReasoningEffort.High,
          Output = ReasoningOutput.Summary,
        },
      },
      AIContextProviders = [
        new BankrollProvider(mediator),
        new MemoriesProvider(unitOfWork),
        new WebSearchProvider(searchService)
      ],
    });

    var session = await agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
    var response = await agent.RunAsync("What are latest FIFA World Cup news?", session);
    return MapResponse(response);
  }

  private static string LoadInstructions()
  {
    var workspace = Path.Combine(AppContext.BaseDirectory, "AI", "Common");
    var path = Path.Combine(workspace, "SOUL.md");
    return File.Exists(path)
      ? $"# SOUL\n\n{File.ReadAllText(path)}"
      : string.Empty;
  }

  private static List<IMessage> MapResponse(AgentResponse response)
  {
    var messages = new List<IMessage>();

    foreach (var chatMessage in response.Messages)
    {
      foreach (var item in chatMessage.Contents)
      {
        switch (item)
        {
          case TextReasoningContent reasoning when !string.IsNullOrEmpty(reasoning.Text):
            messages.Add(new ReasoningMessage(reasoning.Text));
            break;

          case FunctionCallContent functionCall:
            var arguments = functionCall.Arguments?
              .Select(a => new FunctionArgument(a.Key, a.Value?.ToString()))
              .ToList();
            messages.Add(new FunctionMessage(functionCall.Name, arguments));
            break;

          case TextContent text when !string.IsNullOrEmpty(text.Text):
            messages.Add(new Message(text.Text));
            break;
        }
      }
    }

    if (messages.Count == 0 && !string.IsNullOrEmpty(response.Text))
    {
      messages.Add(new Message(response.Text));
    }

    return messages;
  }
}
