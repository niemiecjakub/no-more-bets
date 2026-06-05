using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.AI.Phases.MemoryCleanup;

internal static class MemoryCleanupPhaseTools
{
  public static IReadOnlyList<AITool> CreateStepTools(IPluginFactory factory)
  {
    var search = (InternetSearchPlugin)factory.CreateInternetSearchPlugin();

    return
    [
      .. AgentAiTools.SearchTools(search),
    ];
  }
}
