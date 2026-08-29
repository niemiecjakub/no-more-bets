using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using NoMoreBets.Infrastructure.AI.Providers.DailySlip;
using NoMoreBets.Infrastructure.AI.Providers.Todo;
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
  public string AgentName => "DailySlipAgent";

  public string AgentInstructions => """
    Role: Daily house betting card producer.

    Goal: Build up to three paper slips (Low, Medium, High risk) that maximize expected value for each risk profile.

    Success criteria:
    - At most one slip per risk level; skip a level with no defensible opportunity
    - Each slip has honest win probability (0 < p < 1), rationale, and primary loss risks
    - Slips represent different strategies, not the same legs repeated
    - Closing note lists considered-but-skipped matches, one line each

    Constraints:
    - Only today's available matches with current odds; never invent markets or prices
    - Favor selections where estimated win chance beats implied odds — not favorites alone or long shots alone
    - Low: safer markets, fewer legs; Medium: strongest overall view; High: higher payout, lower win rate — no speculative legs just for odds
    - Account for correlation between related legs
    - Web search only when stored research is thin or late-breaking news (lineup, injury) could change a pick; prefer newer specific evidence over stale research

    Stop: Finish when slips are placed or all levels are honestly skipped.
    """;

  public string BuildPrompt() => """
      Produce today's house betting card from available matches, stored research, and current odds.
      """;

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([
      ToolRegistry.Match.GetClubRollingPerformance,
      ToolRegistry.Match.GetLeagueTable,
      ToolRegistry.Match.GetGroupTable,
    ]);

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new DailySlipProvider(
      serviceProvider.GetRequiredService<DailySlipTool>(),
      serviceProvider.GetRequiredService<BettingTool>()),
    new WebSearchProvider(
      serviceProvider.GetRequiredService<ISearchService>(),
      serviceProvider.GetRequiredService<AgentRunToolMetadataCollector>()),
    new TodoListProvider(),
  ];
}
