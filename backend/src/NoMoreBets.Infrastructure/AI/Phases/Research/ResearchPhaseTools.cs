using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.AI.Phases.Research;

internal static class ResearchPhaseTools
{
  public static IReadOnlyList<AITool> CreatePrimaryStepTools(IPluginFactory factory)
  {
    var match = (MatchPlugin)factory.CreateMatchPlugin();
    var search = (InternetSearchPlugin)factory.CreateInternetSearchPlugin();

    return
    [
      AgentAiTools.Create(match.GetLineupsAsync, "GetLineups"),
      AgentAiTools.Create(match.GetInjuriesAsync, "GetInjuries"),
      AgentAiTools.Create(match.GetHead2HeadStatsAsync, "GetHead2HeadStats"),
      AgentAiTools.Create(match.GetClubDailySummaryAsync, "GetClubDailySummary"),
      AgentAiTools.Create(match.GetClubRecentGamesAsync, "GetClubRecentGames"),
      AgentAiTools.Create(match.GetClubStatistics, "GetClubLeagueStatistics"),
      AgentAiTools.Create(match.GetLeagueTableAsync, "GetLeagueTable"),
      AgentAiTools.Create(match.GetMatchBettingOddsHistoryAsync, "GetMatchBettingOddsHistory"),
      AgentAiTools.Create(match.GetClubRollingPerformanceAsync, "GetClubRollingPerformance"),
      AgentAiTools.Create(match.SaveMatchAnalysisAsync, "SaveMatchAnalysisAsync"),
      .. AgentAiTools.SearchTools(search),
    ];
  }

  public static IReadOnlyList<AITool> CreatePaperBetStepTools(IPluginFactory factory, int matchId)
  {
    var researchBet = (ResearchBetPlugin)factory.CreateResearchBetPlugin(matchId);

    return
    [
      AgentAiTools.Create(researchBet.GetMatchBasicInfoAsync, "GetMatchBasicInfo"),
      AgentAiTools.Create(researchBet.GetMatchEventsAsync, "GetMatchEvents"),
      AgentAiTools.Create(researchBet.PlaceBetSlip, "PlaceBetSlip"),
    ];
  }
}
