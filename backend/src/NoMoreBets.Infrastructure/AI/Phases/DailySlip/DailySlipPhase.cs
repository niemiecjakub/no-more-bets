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

internal sealed class DailySlipExecuteStep : IAgentPhaseStep, IDisposable
{
  private DailySlipBackgroundAgentsHost? _childAgentHost;

  public string AgentName => "DailySlipAgent";

  public bool LoopUntilBackgroundTasksComplete => true;

  public string AgentInstructions => """
    Role: Daily house betting card coordinator.

    Goal: Produce up to three paper slips (Low, Medium, High risk) by briefing specialist agents and placing their recommendations.

    Workflow:
    1. Review today's card (matches, research, odds). Build one shared briefing: fixtures worth considering, key edges, and matches you are skipping with one-line reasons.
    2. Start background tasks on LowRisk, MediumRisk, and HighRisk — all three before waiting — each with the same briefing.
    3. Wait for all specialists to finish. Retrieve each result.
    4. For each placeable JSON return, call dailyslip_placeBetSlip with the returned riskLevel, betSelections, rationale, and estimatedWinProbability. Skip unplaceable returns and honest skips without revising for overlap.
    5. End with a short closing note listing considered-but-skipped matches not already covered.

    Success criteria:
    - At most one slip per risk level placed
    - Each placed slip has honest win probability (0 < p < 1), rationale, and reflects the specialist's view
    - Specialists represent different strategies; do not deconflict overlapping legs

    Constraints:
    - Only today's available matches with current odds; never invent markets or prices
    - You are the only agent that places slips
    - Web search only when stored research is thin or late-breaking news could change the card

    Stop: Finish when placeable slips are placed or all levels are honestly skipped.
    """;

  public string BuildPrompt() => """
      Produce today's house betting card: build a shared briefing, delegate to LowRisk, MediumRisk, and HighRisk specialists, then place their recommendations.
      """;

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([
      ToolRegistry.Match.GetClubRollingPerformance,
      ToolRegistry.Match.GetLeagueTable,
      ToolRegistry.Match.GetGroupTable,
    ]);

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider)
  {
    _childAgentHost?.Dispose();
    _childAgentHost = new DailySlipBackgroundAgentsHost(
      serviceProvider.GetRequiredService<IServiceScopeFactory>(),
      serviceProvider.GetRequiredService<AgentBuilder>());

    return
    [
      new DailySlipProvider(
        serviceProvider.GetRequiredService<DailySlipTool>(),
        serviceProvider.GetRequiredService<BettingTool>(),
        includePlacement: true),
      new WebSearchProvider(
        serviceProvider.GetRequiredService<ISearchService>(),
        serviceProvider.GetRequiredService<AgentRunToolMetadataCollector>()),
      new TodoListProvider(),
      _childAgentHost.Provider,
    ];
  }

  public void Dispose() => _childAgentHost?.Dispose();
}
