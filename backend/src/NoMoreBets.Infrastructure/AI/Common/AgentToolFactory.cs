using System.Reflection;
using Microsoft.Extensions.AI;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.AI.Common;

public static class AgentToolFactory
{
  public static IReadOnlyList<AITool> CreateFromObject(object instance)
  {
    var tools = new List<AITool>();
    var type = instance.GetType();

    foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
    {
      var attribute = method.GetCustomAttribute<AgentToolAttribute>();
      if (attribute is null)
      {
        continue;
      }

      var options = attribute.Name is { Length: > 0 } name
        ? new AIFunctionFactoryOptions { Name = name }
        : null;

      tools.Add(AIFunctionFactory.Create(method, instance, options));
    }

    return tools;
  }
}
