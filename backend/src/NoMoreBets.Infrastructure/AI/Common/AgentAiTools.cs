using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

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
}
