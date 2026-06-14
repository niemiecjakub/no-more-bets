using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common.Dto;

namespace NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;

public sealed class AgentResponseMappingMiddleware
{
  private readonly AgentRunMessageCollector _collector;
  private readonly AgentRunToolMetadataCollector _toolMetadataCollector;

  public AgentResponseMappingMiddleware(
    AgentRunMessageCollector collector,
    AgentRunToolMetadataCollector toolMetadataCollector)
  {
    _collector = collector;
    _toolMetadataCollector = toolMetadataCollector;
  }

  public async Task<AgentResponse> InvokeAsync(
    IEnumerable<ChatMessage> messages,
    AgentSession? session,
    AgentRunOptions? options,
    AIAgent innerAgent,
    CancellationToken cancellationToken)
  {
    _toolMetadataCollector.Reset();

    var response = await innerAgent
      .RunAsync(messages, session, options, cancellationToken)
      .ConfigureAwait(false);

    _collector.SetMessages(Map(response));
    return response;
  }

  private List<IMessage> Map(AgentResponse response)
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
            var metadata = _toolMetadataCollector.TryTake(functionCall.CallId);
            messages.Add(new FunctionMessage(functionCall.Name, arguments, metadata));
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
