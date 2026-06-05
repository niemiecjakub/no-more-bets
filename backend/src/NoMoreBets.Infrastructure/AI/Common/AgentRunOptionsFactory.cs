using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace NoMoreBets.Infrastructure.AI.Common;

internal static class AgentRunOptionsFactory
{
  public static ChatClientAgentRunOptions CreateDefault(bool enableReasoningEffort = false)
  {
    var chatOptions = new ChatOptions
    {
      AllowMultipleToolCalls = true,
      ToolMode = ChatToolMode.Auto,
      Reasoning = new ReasoningOptions
      {
        Effort = ReasoningEffort.Medium,
        Output = ReasoningOutput.Summary,
      },
    };

    return new ChatClientAgentRunOptions(chatOptions);
  }

  public static ChatClientAgentRunOptions WithTools(
    ChatClientAgentRunOptions baseOptions,
    IReadOnlyList<AITool> tools)
  {
    var chatOptions = baseOptions.ChatOptions?.Clone() ?? new ChatOptions();
    chatOptions.Tools = tools as IList<AITool> ?? tools.ToList();
    return new ChatClientAgentRunOptions(chatOptions);
  }
}
