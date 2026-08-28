using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Providers.AgentMode;
using NoMoreBets.Infrastructure.AI.Providers.Memories;
using NoMoreBets.Infrastructure.AI.Providers.Todo;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using NoMoreBets.Infrastructure.AI.Providers.WebSearch;
using NoMoreBets.Infrastructure.AI.Tools;

namespace NoMoreBets.Infrastructure.AI.Phases.Research;

public static class ResearchPhaseDefinition
{
  public static AgentSessionPhase Phase => AgentSessionPhase.Research;
}

internal sealed class ResearchExecuteStep(Match match) : IAgentPhaseStep
{
  public string AgentName => "ResearchAgent";

  public string AgentInstructions => """
    Role: Pre-match football intelligence analyst.

    Goal: Build the most accurate, decision-ready read of how this fixture is likely played.

    Success criteria:
    - MatchOverview, KeyPoints, and RisksAndUnknowns each carry distinct content
    - A later decision-maker can see what matters, why, what is uncertain, and load-bearing assumptions

    Constraints:
    - Do not judge value, prices, or whether to bet
    - Odds history is a signal only — identify what moved the market, then move on
    - Prefer causal, predictive, well-supported evidence; discount speculative, stale, or weakly connected claims
    - Cross-check load-bearing claims; when evidence conflicts or is incomplete, state that instead of forcing a conclusion

    Stop: Submit when material factors are covered or uncertainty is honestly bounded by missing evidence.
    """;

  public string BuildPrompt() => $"""
        Match ID: {match.Id}
        Fixture: {match.HomeClub.Name} vs {match.AwayClub.Name}
        Kickoff (UTC): {match.MatchDate:yyyy-MM-dd HH:mm}

        Research this fixture.
        """;

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider)
  {
    var tools = new List<AgentTool>
    {
      ToolRegistry.Match.GetLineups,
      ToolRegistry.Match.GetInjuries,
      ToolRegistry.Match.GetHead2HeadStats,
      ToolRegistry.Match.GetClubRecentGames,
      ToolRegistry.Match.GetMatchBettingOddsHistory,
      ToolRegistry.Match.GetClubRollingPerformance,
      ToolRegistry.Match.GetClubLeagueStatistics,
    };

    // National teams have no club daily summary. World Cup uses group tables instead of a flat league table.
    if (match.IsFifaWorldCup)
    {
      tools.Add(ToolRegistry.Match.GetGroupTable);
    }
    else
    {
      tools.Add(ToolRegistry.Match.GetClubDailySummary);
      tools.Add(ToolRegistry.Match.GetLeagueTable);
    }

    return serviceProvider.ResolveTools(tools.ToArray());
  }

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new WebSearchProvider(
      serviceProvider.GetRequiredService<ISearchService>(),
      serviceProvider.GetRequiredService<AgentRunToolMetadataCollector>()),
    new AgentModeProvider(),
    new TodoProvider(),
  ];
}

internal sealed class PaperBetFollowUpStep(int matchId) : IAgentPhaseStep
{
  public string AgentName => "ResearchAgent";

  public string AgentInstructions => """
    Role: Research consistency validator.

    Goal: Place a fictional slip that tests whether prior research implies coherent predictions.

    Success criteria:
    - Each selection is a distinct implication of the prior research
    - All legs are mutually consistent with each other and the research conclusions

    Constraints:
    - Use only prior research from this session — no new facts, searches, or outside knowledge
    - Ignore odds, pricing, and value

    Stop: Place the slip once consistency is verified, or report which research conclusions conflict.
    """;

  public string BuildPrompt() => """
    Validate prior research by placing a fictional prediction slip for this match.
    """;

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([
      ToolRegistry.ResearchBet.GetMatchBasicInfo(matchId),
      ToolRegistry.ResearchBet.GetMatchEvents(matchId),
      ToolRegistry.ResearchBet.PlaceBetSlip(matchId),
    ]);
}
