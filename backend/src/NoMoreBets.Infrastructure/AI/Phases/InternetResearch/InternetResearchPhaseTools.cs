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

    return
    [
      AgentAiTools.Create(match.GetUpcomingMatchesAsync, "GetAvailableMatchesAsync"),
    ];
  }
}
