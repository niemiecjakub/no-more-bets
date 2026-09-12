using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace NoMoreBets.Infrastructure.AI.Tools;

internal static class AiToolBind
{
  public static AITool Bind(Delegate method, string name) =>
    AIFunctionFactory.Create(method, new AIFunctionFactoryOptions
    {
      Name = name,
      SerializerOptions = AgentAbstractionsJsonUtilities.DefaultOptions,
    });
}
