using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.AgentTools;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Providers.AgentMode;
using NoMoreBets.Infrastructure.AI.Providers.Memories;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using NoMoreBets.Infrastructure.AI.Providers.Todo;
using NoMoreBets.Infrastructure.AI.Providers.WebSearch;
using NoMoreBets.Infrastructure.AI.Tools;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.AI.Phases.Research;

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
    var matchTool = serviceProvider.GetRequiredService<MatchTool>();
    var tools = new List<AITool>
    {
      AiToolBind.Bind(matchTool.GetLineupsAsync, AgentToolCatalog.Match.GetLineups.Name),
      AiToolBind.Bind(matchTool.GetInjuriesAsync, AgentToolCatalog.Match.GetInjuries.Name),
      AiToolBind.Bind(matchTool.GetHead2HeadStatsAsync, AgentToolCatalog.Match.GetHead2HeadStats.Name),
      AiToolBind.Bind(matchTool.GetClubRecentGamesAsync, AgentToolCatalog.Match.GetClubRecentGames.Name),
      AiToolBind.Bind(matchTool.GetMatchBettingOddsHistoryAsync, AgentToolCatalog.Match.GetMatchBettingOddsHistory.Name),
      AiToolBind.Bind(matchTool.GetClubRollingPerformanceAsync, AgentToolCatalog.Match.GetClubRollingPerformance.Name),
      AiToolBind.Bind(matchTool.GetClubStatistics, AgentToolCatalog.Match.GetClubLeagueStatistics.Name),
    };

    // National teams have no club daily summary. World Cup uses group tables instead of a flat league table.
    if (match.IsFifaWorldCup)
    {
      tools.Add(AiToolBind.Bind(matchTool.GetGroupTableAsync, AgentToolCatalog.Match.GetGroupTable.Name));
    }
    else
    {
      tools.Add(AiToolBind.Bind(matchTool.GetClubDailySummaryAsync, AgentToolCatalog.Match.GetClubDailySummary.Name));
      tools.Add(AiToolBind.Bind(matchTool.GetLeagueTableAsync, AgentToolCatalog.Match.GetLeagueTable.Name));
    }

    return tools;
  }

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new WebSearchProvider(
      serviceProvider.GetRequiredService<ISearchService>(),
      serviceProvider.GetRequiredService<AgentRunToolMetadataCollector>()),
    new AgenticModeProvider(),
    new TodoListProvider(),
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

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider)
  {
    var researchBet = ActivatorUtilities.CreateInstance<ResearchBetTool>(serviceProvider, matchId);
    return
    [
      AiToolBind.Bind(researchBet.GetMatchBasicInfoAsync, AgentToolCatalog.ResearchBet.GetMatchBasicInfo.Name),
      AiToolBind.Bind(researchBet.GetMatchEventsAsync, AgentToolCatalog.ResearchBet.GetMatchEvents.Name),
      AiToolBind.Bind(researchBet.PlaceBetSlip, AgentToolCatalog.ResearchBet.PlaceBetSlip.Name),
    ];
  }
}
