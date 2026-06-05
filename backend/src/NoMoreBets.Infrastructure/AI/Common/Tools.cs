using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.AI.Common;

public static class Tools
{
  public static class Match
  {
    public static readonly AgentTool GetLineups =
      FromMatch("GetLineups", p => p.GetLineupsAsync);

    public static readonly AgentTool GetInjuries =
      FromMatch("GetInjuries", p => p.GetInjuriesAsync);

    public static readonly AgentTool GetHead2HeadStats =
      FromMatch("GetHead2HeadStats", p => p.GetHead2HeadStatsAsync);

    public static readonly AgentTool GetClubDailySummary =
      FromMatch("GetClubDailySummary", p => p.GetClubDailySummaryAsync);

    public static readonly AgentTool GetClubRecentGames =
      FromMatch("GetClubRecentGames", p => p.GetClubRecentGamesAsync);

    public static readonly AgentTool GetClubLeagueStatistics =
      FromMatch("GetClubLeagueStatistics", p => p.GetClubStatistics);

    public static readonly AgentTool GetLeagueTable =
      FromMatch("GetLeagueTable", p => p.GetLeagueTableAsync);

    public static readonly AgentTool GetMatchBettingOddsHistory =
      FromMatch("GetMatchBettingOddsHistory", p => p.GetMatchBettingOddsHistoryAsync);

    public static readonly AgentTool GetClubRollingPerformance =
      FromMatch("GetClubRollingPerformance", p => p.GetClubRollingPerformanceAsync);

    public static readonly AgentTool SaveMatchAnalysis =
      FromMatch("SaveMatchAnalysisAsync", p => p.SaveMatchAnalysisAsync);

    public static readonly AgentTool GetMatchResearchText =
      FromMatch("GetMatchResearchTextAsync", p => p.GetMatchResearchTextAsync);

    public static readonly AgentTool GetUpcomingMatches =
      FromMatch("GetAvailableMatchesAsync", p => p.GetUpcomingMatchesAsync);
  }

  public static class Betting
  {
    public static readonly AgentTool GetAvailableMatches =
      FromBetting("GetAvailableMatches", p => p.GetAvailableMatchesAsync);

    public static readonly AgentTool GetCurrentOdds =
      FromBetting("GetCurrentOdds", p => p.GetCurrentOddsAsync);

    public static readonly AgentTool GetMatchAnalysis =
      FromBetting("GetMatchAnalysis", p => p.GetMatchAnalysisAsync);

    public static readonly AgentTool PlaceBetSlip =
      FromBetting("PlaceBetSlip", p => p.PlaceBetSlip);

    public static readonly AgentTool GetBetSlips =
      FromBetting("GetBetSlips", p => p.GetBetSlipsAsync);

    public static readonly AgentTool GetBetSlipsAwaitingReflection =
      FromBetting("GetBetSlipsAwaitingReflectionAsync", p => p.GetBetSlipsAwaitingReflectionAsync);
  }

  public static class SocialMedia
  {
    public static readonly AgentTool CreateXPost =
      FromSocialMedia("CreateXPost", p => p.CreateXPostAsync);
  }

  public static class ResearchBet
  {
    public static AgentTool GetMatchBasicInfo(int matchId) =>
      new(ctx => AgentAiTools.Create(ctx.ResearchBet(matchId).GetMatchBasicInfoAsync, "GetMatchBasicInfo"));

    public static AgentTool GetMatchEvents(int matchId) =>
      new(ctx => AgentAiTools.Create(ctx.ResearchBet(matchId).GetMatchEventsAsync, "GetMatchEvents"));

    public static AgentTool PlaceBetSlip(int matchId) =>
      new(ctx => AgentAiTools.Create(ctx.ResearchBet(matchId).PlaceBetSlip, "PlaceBetSlip"));
  }

  private static AgentTool FromMatch(string name, Func<MatchPlugin, Delegate> method) =>
    new(ctx => AgentAiTools.Create(method(ctx.Match), name));

  private static AgentTool FromBetting(string name, Func<BettingPlugin, Delegate> method) =>
    new(ctx => AgentAiTools.Create(method(ctx.Betting), name));

  private static AgentTool FromSocialMedia(string name, Func<SocialMediaPlugin, Delegate> method) =>
    new(ctx => AgentAiTools.Create(method(ctx.SocialMedia), name));
}
