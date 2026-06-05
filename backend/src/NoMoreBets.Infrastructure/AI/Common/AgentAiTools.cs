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

  internal static IEnumerable<AITool> MemoryTools(MemoriesPlugin memories) =>
  [
    Create(memories.GetMemoryRecordsAsync, "GetMemoryRecordsAsync"),
    Create(memories.ReadAsync, "ReadMemoryAsync"),
    Create(memories.WriteAsync, "WriteMemoryAsync"),
    Create(memories.AppendAsync, "AppendMemoryAsync"),
    Create(memories.ReplaceAsync, "ReplaceMemoryAsync"),
  ];

  internal static IEnumerable<AITool> MemoryMaintenanceTools(MemoriesPlugin memories) =>
  [
    .. MemoryTools(memories),
    Create(memories.DeleteMemoryAsync, "DeleteMemoryAsync"),
  ];

  internal static IEnumerable<AITool> SearchTools(InternetSearchPlugin search) =>
  [
    Create(search.SearchNewsAsync, "SearchNewsAsync"),
    Create(search.GetWebGroundingAsync, "GetWebGroundingAsync"),
  ];

  internal static IEnumerable<AITool> BankrollTools(BankrollPlugin bankroll) =>
  [
    Create(bankroll.GetCurrentBalanceAsync, "GetCurrentBalance"),
    Create(bankroll.GetDaysUntilPaydayAsync, "GetDaysUntilPayday"),
  ];
}
