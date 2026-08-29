using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NoMoreBets.Application.AgentTools;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.AI.Tools;

public static class ToolRegistry
{
  public static class Match
  {
    public static readonly AgentTool GetLineups =
      FromMatch(AgentToolCatalog.Match.GetLineups, p => p.GetLineupsAsync);

    public static readonly AgentTool GetInjuries =
      FromMatch(AgentToolCatalog.Match.GetInjuries, p => p.GetInjuriesAsync);

    public static readonly AgentTool GetHead2HeadStats =
      FromMatch(AgentToolCatalog.Match.GetHead2HeadStats, p => p.GetHead2HeadStatsAsync);

    public static readonly AgentTool GetClubDailySummary =
      FromMatch(AgentToolCatalog.Match.GetClubDailySummary, p => p.GetClubDailySummaryAsync);

    public static readonly AgentTool GetClubRecentGames =
      FromMatch(AgentToolCatalog.Match.GetClubRecentGames, p => p.GetClubRecentGamesAsync);

    public static readonly AgentTool GetClubLeagueStatistics =
      FromMatch(AgentToolCatalog.Match.GetClubLeagueStatistics, p => p.GetClubStatistics);

    public static readonly AgentTool GetLeagueTable =
      FromMatch(AgentToolCatalog.Match.GetLeagueTable, p => p.GetLeagueTableAsync);

    public static readonly AgentTool GetGroupTable =
      FromMatch(AgentToolCatalog.Match.GetGroupTable, p => p.GetGroupTableAsync);

    public static readonly AgentTool GetMatchBettingOddsHistory =
      FromMatch(AgentToolCatalog.Match.GetMatchBettingOddsHistory, p => p.GetMatchBettingOddsHistoryAsync);

    public static readonly AgentTool GetClubRollingPerformance =
      FromMatch(AgentToolCatalog.Match.GetClubRollingPerformance, p => p.GetClubRollingPerformanceAsync);

    public static readonly AgentTool SaveMatchAnalysis =
      FromMatch(AgentToolCatalog.Match.SaveMatchAnalysis, p => p.SaveMatchAnalysisAsync);

    public static readonly AgentTool GetMatchResearchText =
      FromMatch(AgentToolCatalog.Match.GetMatchResearchText, p => p.GetMatchResearchTextAsync);

    public static readonly AgentTool GetUpcomingMatches =
      FromMatch(AgentToolCatalog.Match.GetUpcomingMatches, p => p.GetUpcomingMatchesAsync);
  }

  public static class Betting
  {
    public static readonly AgentTool GetAvailableMatches =
      FromBetting(AgentToolCatalog.Betting.GetAvailableMatches, p => p.GetAvailableMatchesAsync);

    public static readonly AgentTool GetCurrentOdds =
      FromBetting(AgentToolCatalog.Betting.GetCurrentOdds, p => p.GetCurrentOddsAsync);

    public static readonly AgentTool GetCurrentOddsForMarket =
      FromBetting(AgentToolCatalog.Betting.GetCurrentOddsForMarket, p => p.GetCurrentOddsForMarketAsync);

    public static readonly AgentTool GetMatchAnalysis =
      FromBetting(AgentToolCatalog.Betting.GetMatchAnalysis, p => p.GetMatchAnalysisAsync);

    public static readonly AgentTool PlaceBetSlip =
      FromBetting(AgentToolCatalog.Betting.PlaceBetSlip, p => p.PlaceBetSlip);

    public static readonly AgentTool GetBetSlips =
      FromBetting(AgentToolCatalog.Betting.GetBetSlips, p => p.GetBetSlipsAsync);

    public static readonly AgentTool GetBetSlipsAwaitingReflection =
      FromBetting(AgentToolCatalog.Betting.GetBetSlipsAwaitingReflection, p => p.GetBetSlipsAwaitingReflectionAsync);
  }

  public static class DailySlip
  {
    public static readonly AgentTool PlaceBetSlip =
      FromDailySlip(AgentToolCatalog.DailySlip.PlaceBetSlip, p => p.PlaceBetSlip);
  }

  public static class SocialMedia
  {
    public static readonly AgentTool CreateXPost =
      FromSocialMedia(AgentToolCatalog.SocialMedia.CreateXPost, p => p.CreateXPostAsync);
  }

  public static class ResearchBet
  {
    public static AgentTool GetMatchBasicInfo(int matchId) =>
      new(ctx => Create(ctx.ResearchBet(matchId).GetMatchBasicInfoAsync, AgentToolCatalog.ResearchBet.GetMatchBasicInfo.Name));

    public static AgentTool GetMatchEvents(int matchId) =>
      new(ctx => Create(ctx.ResearchBet(matchId).GetMatchEventsAsync, AgentToolCatalog.ResearchBet.GetMatchEvents.Name));

    public static AgentTool PlaceBetSlip(int matchId) =>
      new(ctx => Create(ctx.ResearchBet(matchId).PlaceBetSlip, AgentToolCatalog.ResearchBet.PlaceBetSlip.Name));
  }

  private static AgentTool FromMatch(AgentToolDefinition definition, Func<MatchTool, Delegate> method) =>
    new(ctx => Create(method(ctx.Match), definition.Name));

  private static AgentTool FromBetting(AgentToolDefinition definition, Func<BettingTool, Delegate> method) =>
    new(ctx => Create(method(ctx.Betting), definition.Name));

  private static AgentTool FromDailySlip(AgentToolDefinition definition, Func<DailySlipTool, Delegate> method) =>
    new(ctx => Create(method(ctx.DailySlip), definition.Name));

  private static AgentTool FromSocialMedia(AgentToolDefinition definition, Func<SocialMediaTool, Delegate> method) =>
    new(ctx => Create(method(ctx.SocialMedia), definition.Name));

  internal static AITool Create(Delegate method, string name) =>
    AIFunctionFactory.Create(method, new AIFunctionFactoryOptions
    {
      Name = name,
      SerializerOptions = AgentAbstractionsJsonUtilities.DefaultOptions,
    });
}
