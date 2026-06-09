using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace NoMoreBets.Infrastructure.AI.Tools;

public static class AgentToolResolver
{
  public static IReadOnlyList<AITool> ResolveTools(this IServiceProvider serviceProvider, ReadOnlySpan<AgentTool> tools)
  {
    if (tools.Length == 0)
    {
      return [];
    }

    var context = new PluginToolContext(serviceProvider);
    var result = new AITool[tools.Length];
    for (var i = 0; i < tools.Length; i++)
    {
      result[i] = tools[i].Resolve(context);
    }

    return result;
  }
}
