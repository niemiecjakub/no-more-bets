using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.AI.Phases.MemoryCleanup;

internal static class MemoryCleanupPhaseTools
{
  public static IReadOnlyList<AITool> CreateStepTools(IPluginFactory factory)
  {
    var memories = (MemoriesPlugin)factory.CreateMemoriesPlugin();
    var search = (InternetSearchPlugin)factory.CreateInternetSearchPlugin();
    var bankroll = (BankrollPlugin)factory.CreateBankrollPlugin();

    return
    [
      .. AgentAiTools.MemoryMaintenanceTools(memories),
      .. AgentAiTools.SearchTools(search),
      .. AgentAiTools.BankrollTools(bankroll),
    ];
  }
}
