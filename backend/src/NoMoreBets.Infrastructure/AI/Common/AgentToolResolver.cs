using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.AI.Common;

public static class AgentToolResolver
{
  public static IReadOnlyList<AITool> ResolveTools(this IPluginFactory factory, ReadOnlySpan<AgentTool> tools)
  {
    if (tools.Length == 0)
    {
      return [];
    }

    var context = new PluginToolContext(factory);
    var result = new AITool[tools.Length];
    for (var i = 0; i < tools.Length; i++)
    {
      result[i] = tools[i].Resolve(context);
    }

    return result;
  }
}
