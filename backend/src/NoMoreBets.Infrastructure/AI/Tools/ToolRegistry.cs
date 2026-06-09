using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.AI.Tools;

public static class ToolRegistry
{
  public static class Match
  {
    public static readonly AgentTool GetLineups =
      FromMatch("Match_GetLineups", p => p.GetLineupsAsync);

    public static readonly AgentTool GetInjuries =
      FromMatch("Match_GetInjuries", p => p.GetInjuriesAsync);

    public static readonly AgentTool GetHead2HeadStats =
      FromMatch("Match_GetHead2HeadStats", p => p.GetHead2HeadStatsAsync);

    public static readonly AgentTool GetClubDailySummary =
      FromMatch("Match_GetClubDailySummary", p => p.GetClubDailySummaryAsync);

    public static readonly AgentTool GetClubRecentGames =
      FromMatch("Match_GetClubRecentGames", p => p.GetClubRecentGamesAsync);

    public static readonly AgentTool GetClubLeagueStatistics =
      FromMatch("Match_GetClubLeagueStatistics", p => p.GetClubStatistics);

    public static readonly AgentTool GetLeagueTable =
      FromMatch("Match_GetLeagueTable", p => p.GetLeagueTableAsync);

    public static readonly AgentTool GetMatchBettingOddsHistory =
      FromMatch("Match_GetMatchBettingOddsHistory", p => p.GetMatchBettingOddsHistoryAsync);

    public static readonly AgentTool GetClubRollingPerformance =
      FromMatch("Match_GetClubRollingPerformance", p => p.GetClubRollingPerformanceAsync);

    public static readonly AgentTool SaveMatchAnalysis =
      FromMatch("Match_SaveMatchAnalysisAsync", p => p.SaveMatchAnalysisAsync);

    public static readonly AgentTool GetMatchResearchText =
      FromMatch("Match_GetMatchResearchTextAsync", p => p.GetMatchResearchTextAsync);

    public static readonly AgentTool GetUpcomingMatches =
      FromMatch("Match_GetAvailableMatchesAsync", p => p.GetUpcomingMatchesAsync);
  }

  public static class Betting
  {
    public static readonly AgentTool GetAvailableMatches =
      FromBetting("Betting_GetAvailableMatches", p => p.GetAvailableMatchesAsync);

    public static readonly AgentTool GetCurrentOdds =
      FromBetting("Betting_GetCurrentOdds", p => p.GetCurrentOddsAsync);

    public static readonly AgentTool GetMatchAnalysis =
      FromBetting("Betting_GetMatchAnalysis", p => p.GetMatchAnalysisAsync);

    public static readonly AgentTool PlaceBetSlip =
      FromBetting("Betting_PlaceBetSlip", p => p.PlaceBetSlip);

    public static readonly AgentTool GetBetSlips =
      FromBetting("Betting_GetBetSlips", p => p.GetBetSlipsAsync);

    public static readonly AgentTool GetBetSlipsAwaitingReflection =
      FromBetting("Betting_GetBetSlipsAwaitingReflectionAsync", p => p.GetBetSlipsAwaitingReflectionAsync);
  }

  public static class SocialMedia
  {
    public static readonly AgentTool CreateXPost =
      FromSocialMedia("SocialMedia_CreateXPost", p => p.CreateXPostAsync);
  }

  public static class ResearchBet
  {
    public static AgentTool GetMatchBasicInfo(int matchId) =>
      new(ctx => Create(ctx.ResearchBet(matchId).GetMatchBasicInfoAsync, "ResearchBet_GetMatchBasicInfo"));

    public static AgentTool GetMatchEvents(int matchId) =>
      new(ctx => Create(ctx.ResearchBet(matchId).GetMatchEventsAsync, "ResearchBet_GetMatchEvents"));

    public static AgentTool PlaceBetSlip(int matchId) =>
      new(ctx => Create(ctx.ResearchBet(matchId).PlaceBetSlip, "ResearchBet_PlaceBetSlip"));
  }

  private static AgentTool FromMatch(string name, Func<MatchTool, Delegate> method) =>
    new(ctx => Create(method(ctx.Match), name));

  private static AgentTool FromBetting(string name, Func<BettingTool, Delegate> method) =>
    new(ctx => Create(method(ctx.Betting), name));

  private static AgentTool FromSocialMedia(string name, Func<SocialMediaTool, Delegate> method) =>
    new(ctx => Create(method(ctx.SocialMedia), name));

  internal static AITool Create(Delegate method, string name) =>
    AIFunctionFactory.Create(method, new AIFunctionFactoryOptions
    {
      Name = name,
      SerializerOptions = AgentAbstractionsJsonUtilities.DefaultOptions,
    });
}
