using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using NoMoreBets.Infrastructure.AI.Providers.DailySlip;
using NoMoreBets.Infrastructure.AI.Providers.Date;
using NoMoreBets.Infrastructure.AI.Providers.WebSearch;
using NoMoreBets.Infrastructure.AI.Tools;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.AI.Phases.DailySlip;

public static class DailySlipPhaseDefinition
{
  public static AgentSessionPhase Phase => AgentSessionPhase.DailySlip;
}

internal sealed class DailySlipExecuteStep : IAgentPhaseStep
{
  public string? AgentInstructions => """
    You produce a daily house betting card. You are not Chandler and you do not manage a live bankroll.

    Job: from matches kicking off today, place up to three paper slips — one Low, one Medium, one High.
    Skip a tier rather than inventing filler. If nothing is backable, place nothing and say why.

    Use only matches returned by the available-matches tool. Research only those. Stake is always 10.
    Set estimated win probability honestly. Copy market and option names from current odds.
    """;

  public string BuildPrompt()
  {
    var today = WarsawCalendar.DateFromUtc(DateTime.UtcNow);
    return $"""
      Today's card date (Warsaw): {today:yyyy-MM-dd}.

      1. List today's available matches.
      2. Read research and current odds for fixtures you might use. Check form and tables when they would change a pick. Use web search when stored research is thin or stale.
      3. Place at most one Low, one Medium, and one High paper slip. Skip a tier you cannot defend.
      """;
  }

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([
      ToolRegistry.Match.GetClubRollingPerformance,
      ToolRegistry.Match.GetLeagueTable,
      ToolRegistry.Match.GetGroupTable,
    ]);

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new DateProvider(),
    new DailySlipProvider(
      serviceProvider.GetRequiredService<DailySlipTool>(),
      serviceProvider.GetRequiredService<BettingTool>()),
    new WebSearchProvider(
      serviceProvider.GetRequiredService<ISearchService>(),
      serviceProvider.GetRequiredService<AgentRunToolMetadataCollector>()),
  ];
}
