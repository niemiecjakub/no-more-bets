using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using NoMoreBets.Infrastructure.AI.Providers.DailySlip;
using NoMoreBets.Infrastructure.AI.Providers.Todo;
using NoMoreBets.Infrastructure.AI.Providers.WebSearch;
using NoMoreBets.Infrastructure.AI.Tools;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.AI.Phases.DailySlip;

internal sealed class DailySlipBackgroundAgentsHost : IDisposable
{
  private readonly List<IServiceScope> _scopes = [];

  public BackgroundAgentsProvider Provider { get; }

  public DailySlipBackgroundAgentsHost(
    IServiceScopeFactory scopeFactory,
    AgentBuilder agentBuilder)
  {
    var children = new List<AIAgent>();
    foreach (var definition in ChildDefinitions)
    {
      var scope = scopeFactory.CreateScope();
      _scopes.Add(scope);
      var sp = scope.ServiceProvider;

      var matchTools = sp.ResolveTools([
        ToolRegistry.Match.GetClubRollingPerformance,
        ToolRegistry.Match.GetLeagueTable,
        ToolRegistry.Match.GetGroupTable,
      ]);

      var contextProviders = new AIContextProvider[]
      {
        new DailySlipProvider(
          sp.GetRequiredService<DailySlipTool>(),
          sp.GetRequiredService<BettingTool>(),
          includePlacement: false),
        new WebSearchProvider(
          sp.GetRequiredService<ISearchService>(),
          sp.GetRequiredService<AgentRunToolMetadataCollector>()),
        new TodoListProvider(),
      };

      children.Add(agentBuilder.CreateChildAgent(
        definition.Name,
        definition.Instructions,
        definition.Description,
        contextProviders,
        matchTools));
    }

    Provider = new BackgroundAgentsProvider(children);
  }

  public void Dispose()
  {
    foreach (var scope in _scopes)
    {
      scope.Dispose();
    }

    _scopes.Clear();
  }

  private static readonly IReadOnlyList<ChildAgentDefinition> ChildDefinitions =
  [
    new(
      Name: "LowRisk",
      Description: "Builds the Low-risk daily slip — safer markets, fewer legs.",
      Instructions: BuildChildInstructions(BetRiskLevel.Low, """
        Low risk: favor safer markets and fewer legs. Prioritize win rate over payout.
        Prefer singles or small doubles with correlated risk understood.
        """)),
    new(
      Name: "MediumRisk",
      Description: "Builds the Medium-risk daily slip — strongest overall view.",
      Instructions: BuildChildInstructions(BetRiskLevel.Medium, """
        Medium risk: your best overall view of the card. Balance edge and win probability.
        """)),
    new(
      Name: "HighRisk",
      Description: "Builds the High-risk daily slip — higher payout, lower win rate.",
      Instructions: BuildChildInstructions(BetRiskLevel.High, """
        High risk: higher payout acceptable with lower win rate, but no speculative legs added only for odds.
        """)),
  ];

  private static string BuildChildInstructions(BetRiskLevel riskLevel, string riskProfile) =>
      $$"""
        Role: Daily slip specialist for {{riskLevel}} risk.

        {{riskProfile}}

        You receive a shared briefing from the coordinator. Use tools to deepen your read when stored research is thin or late-breaking news could change a pick.

        Constraints:
        - Only today's available matches with current odds; never invent markets or prices
        - Favor selections where estimated win chance beats implied odds
        - Aim for a strategy distinct from the other risk tiers (do not copy legs just because they look good elsewhere)
        - Web search only when needed; prefer newer specific evidence over stale research

        Output: Return either an honest skip (plain text explaining why no {{riskLevel}} slip today) or a single JSON object with no markdown fencing:
        {"riskLevel":"{{riskLevel}}","betSelections":[{"matchId":0,"eventType":"","eventOption":""}],"rationale":"","estimatedWinProbability":0.0}

        Use real matchId, eventType, and eventOption from tools. estimatedWinProbability must be between 0 and 1 (exclusive).
        Do not place bets — the coordinator places them.
        """;

  private sealed record ChildAgentDefinition(string Name, string Description, string Instructions);
}
