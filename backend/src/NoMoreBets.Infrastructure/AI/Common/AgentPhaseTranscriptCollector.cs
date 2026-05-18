using Microsoft.SemanticKernel;
using NoMoreBets.Application.Common.Dto;

namespace NoMoreBets.Infrastructure.AI.Common;

internal static class AgentPhaseTranscriptCollector
{
  public static async Task<List<IMessage>> CollectAsync(
    AgentConfig config,
    string prompt,
    CancellationToken cancellationToken)
  {
    var messages = new List<IMessage>();
    await foreach (var message in config.Agent.InvokeAsync(prompt, config.Thread, config.Options, cancellationToken)
                     .ConfigureAwait(false))
    {
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
      foreach (var item in message.Message.Items)
      {
        if (item is ReasoningContent reasoning)
        {
          messages.Add(new ReasoningMessage(reasoning.Text));
        }

        if (item is FunctionCallContent functionCall)
        {
          var functionName = functionCall.FunctionName;
          var arguments = functionCall.Arguments?.Select(a => new FunctionArgument(a.Key.ToString(), a.Value?.ToString())).ToList();
          messages.Add(new FunctionMessage(functionName, arguments));
        }
      }

#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
      if (!string.IsNullOrEmpty(message.Message.Content))
      {
        messages.Add(new Message(message.Message.Content));
      }
    }

    return messages;
  }
}
