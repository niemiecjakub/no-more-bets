using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.AI.Common;

internal static class AgentAiTools
{
  internal static JsonSerializerOptions SerializerOptions =>
    AgentAbstractionsJsonUtilities.DefaultOptions;

  internal static AITool Create(Delegate method, string name) =>
    AIFunctionFactory.Create(method, new AIFunctionFactoryOptions
    {
      Name = name,
      SerializerOptions = SerializerOptions,
    });

  internal static IEnumerable<AITool> SearchTools(InternetSearchPlugin search) =>
  [
    Create(search.SearchNewsAsync, "SearchNewsAsync"),
    Create(search.GetWebGroundingAsync, "GetWebGroundingAsync"),
  ];
}
