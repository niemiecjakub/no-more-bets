using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.AI.Phases.Reflection;

internal static class ReflectionPhaseTools
{
  public static IReadOnlyList<AITool> CreateStepTools(IPluginFactory factory)
  {
    var betting = (BettingPlugin)factory.CreateBettingPlugin();
    var match = (MatchPlugin)factory.CreateMatchPlugin();

    return
    [
      AgentAiTools.Create(betting.GetBetSlipsAwaitingReflectionAsync, "GetBetSlipsAwaitingReflectionAsync"),
      AgentAiTools.Create(match.GetMatchResearchTextAsync, "GetMatchResearchTextAsync"),
    ];
  }
}
