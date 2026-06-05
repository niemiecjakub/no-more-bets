using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.AI.Phases.Betting;

internal static class BettingPhaseTools
{
  public static IReadOnlyList<AITool> CreatePrimaryStepTools(IPluginFactory factory)
  {
    var betting = (BettingPlugin)factory.CreateBettingPlugin();
    var search = (InternetSearchPlugin)factory.CreateInternetSearchPlugin();

    return
    [
      AgentAiTools.Create(betting.GetAvailableMatchesAsync, "GetAvailableMatches"),
      AgentAiTools.Create(betting.GetCurrentOddsAsync, "GetCurrentOdds"),
      AgentAiTools.Create(betting.GetMatchAnalysisAsync, "GetMatchAnalysis"),
      AgentAiTools.Create(betting.PlaceBetSlip, "PlaceBetSlip"),
      AgentAiTools.Create(betting.GetBetSlipsAsync, "GetBetSlips"),
      .. AgentAiTools.SearchTools(search),
    ];
  }

  public static IReadOnlyList<AITool> CreateXPostStepTools(IPluginFactory factory)
  {
    var social = (SocialMediaPlugin)factory.CreateSocialMediaPlugin();

    return [AgentAiTools.Create(social.CreateXPostAsync, "CreateXPost")];
  }
}
