using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.AI.Phases.InternetResearch;

internal static class InternetResearchPhaseTools
{
  public static IReadOnlyList<AITool> CreateStepTools(IPluginFactory factory)
  {
    var match = (MatchPlugin)factory.CreateMatchPlugin();
    var memories = (MemoriesPlugin)factory.CreateMemoriesPlugin();
    var search = (InternetSearchPlugin)factory.CreateInternetSearchPlugin();
    var bankroll = (BankrollPlugin)factory.CreateBankrollPlugin();

    return
    [
      AgentAiTools.Create(match.GetUpcomingMatchesAsync, "GetAvailableMatchesAsync"),
      .. AgentAiTools.MemoryTools(memories),
      .. AgentAiTools.SearchTools(search),
      .. AgentAiTools.BankrollTools(bankroll),
    ];
  }
}
