using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common.Dto;

namespace NoMoreBets.Infrastructure.AI.Common;

internal static class AgentPhaseTranscriptCollector
{
  public static async Task<List<IMessage>> CollectAsync(
    AgentConfig config,
    string prompt,
    IReadOnlyList<AITool> tools,
    CancellationToken cancellationToken)
  {
    var runOptions = AgentRunOptionsFactory.WithTools(config.DefaultRunOptions, tools);
    var response = await config.Agent
      .RunAsync(prompt, config.Session, runOptions, cancellationToken)
      .ConfigureAwait(false);

    return MapResponse(response);
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
