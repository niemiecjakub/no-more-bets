using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.AI.Tools;

public static class ToolRegistry
{
  public static class Match
  {
    public static readonly AgentTool GetLineups =
      FromMatch("match_getLineups", p => p.GetLineupsAsync);

    public static readonly AgentTool GetInjuries =
      FromMatch("match_getInjuries", p => p.GetInjuriesAsync);

    public static readonly AgentTool GetHead2HeadStats =
      FromMatch("match_getHead2HeadStats", p => p.GetHead2HeadStatsAsync);

    public static readonly AgentTool GetClubDailySummary =
      FromMatch("match_getClubDailySummary", p => p.GetClubDailySummaryAsync);

    public static readonly AgentTool GetClubRecentGames =
      FromMatch("match_getClubRecentGames", p => p.GetClubRecentGamesAsync);

    public static readonly AgentTool GetClubLeagueStatistics =
      FromMatch("match_getClubLeagueStatistics", p => p.GetClubStatistics);

    public static readonly AgentTool GetLeagueTable =
      FromMatch("match_getLeagueTable", p => p.GetLeagueTableAsync);

    public static readonly AgentTool GetGroupTable =
      FromMatch("match_getGroupTable", p => p.GetGroupTableAsync);

    public static readonly AgentTool GetMatchBettingOddsHistory =
      FromMatch("match_getMatchBettingOddsHistory", p => p.GetMatchBettingOddsHistoryAsync);

    public static readonly AgentTool GetClubRollingPerformance =
      FromMatch("match_getClubRollingPerformance", p => p.GetClubRollingPerformanceAsync);

    public static readonly AgentTool SaveMatchAnalysis =
      FromMatch("match_saveMatchAnalysisAsync", p => p.SaveMatchAnalysisAsync);

    public static readonly AgentTool GetMatchResearchText =
      FromMatch("match_getMatchResearchTextAsync", p => p.GetMatchResearchTextAsync);

    public static readonly AgentTool GetUpcomingMatches =
      FromMatch("match_getAvailableMatchesAsync", p => p.GetUpcomingMatchesAsync);
  }

  public static class Betting
  {
    public static readonly AgentTool GetAvailableMatches =
      FromBetting("betting_getAvailableMatches", p => p.GetAvailableMatchesAsync);

    public static readonly AgentTool GetCurrentOdds =
      FromBetting("betting_getCurrentOdds", p => p.GetCurrentOddsAsync);

    public static readonly AgentTool GetMatchAnalysis =
      FromBetting("betting_getMatchAnalysis", p => p.GetMatchAnalysisAsync);

    public static readonly AgentTool PlaceBetSlip =
      FromBetting("betting_placeBetSlip", p => p.PlaceBetSlip);

    public static readonly AgentTool GetBetSlips =
      FromBetting("betting_getBetSlips", p => p.GetBetSlipsAsync);

    public static readonly AgentTool GetBetSlipsAwaitingReflection =
      FromBetting("betting_getBetSlipsAwaitingReflectionAsync", p => p.GetBetSlipsAwaitingReflectionAsync);
  }

  public static class SocialMedia
  {
    public static readonly AgentTool CreateXPost =
      FromSocialMedia("socialmedia_createXPost", p => p.CreateXPostAsync);
  }

  public static class ResearchBet
  {
    public static AgentTool GetMatchBasicInfo(int matchId) =>
      new(ctx => Create(ctx.ResearchBet(matchId).GetMatchBasicInfoAsync, "researchbet_getMatchBasicInfo"));

    public static AgentTool GetMatchEvents(int matchId) =>
      new(ctx => Create(ctx.ResearchBet(matchId).GetMatchEventsAsync, "researchbet_getMatchEvents"));

    public static AgentTool PlaceBetSlip(int matchId) =>
      new(ctx => Create(ctx.ResearchBet(matchId).PlaceBetSlip, "researchbet_placeBetSlip"));
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
