using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.AI.Providers.AgentMode;
using NoMoreBets.Infrastructure.AI.Providers.Date;
using NoMoreBets.Infrastructure.AI.Providers.Memories;
using NoMoreBets.Infrastructure.AI.Providers.Todo;
using NoMoreBets.Infrastructure.AI.Providers.WebSearch;
using NoMoreBets.Infrastructure.AI.Tools;

namespace NoMoreBets.Infrastructure.AI.Phases.Research;

public static class ResearchPhaseDefinition
{
  public static AgentSessionPhase Phase => AgentSessionPhase.Research;
}

internal sealed class ResearchExecuteStep(Match match) : IAgentPhaseStep
{
  public string BuildPrompt() => $"""
        Match ID: {match.Id}   
        Fixture: {match.HomeClub.Name} vs {match.AwayClub.Name}
        Kickoff (UTC): {match.MatchDate:yyyy-MM-dd HH:mm}

        Conduct pre-match intelligence gathering for a betting research system.
        Your objective is to build the most accurate possible understanding of this match.

        You are responsible for deciding what information is relevant, how deeply it should be investigated, and which sources deserve trust.
        Approach the task as an investigator rather than a summarizer.
        Actively search for the factors most likely to influence the outcome of the match. Determine which factors are genuinely material to this fixture rather than following a fixed research template.
        Distinguish signal from noise. Give more weight to information that is predictive and well-supported, and less weight to information that is speculative, anecdotal, stale, or weakly connected to match outcomes.
        Prioritize causal drivers over descriptive facts. Explain not only what is true, but why it matters for this matchup.
        Focus on synthesis rather than accumulation. The goal is not to gather the most information, but to identify and explain the information most likely to affect interpretation of the match.
        When evidence is incomplete, conflicting, or uncertain, represent that uncertainty explicitly rather than forcing a conclusion.

        The final output should allow a future decision-maker to quickly understand:
        - what matters most in this match,
        - why it matters,
        - what remains uncertain,
        - and which assumptions the current understanding depends on.
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
    };

    // National teams have no club daily summary, and a tournament has no
    // league statistics or league table, so skip those tools for World Cup matches.
    if (!match.IsFifaWorldCup)
    {
      tools.Add(ToolRegistry.Match.GetClubDailySummary);
      tools.Add(ToolRegistry.Match.GetClubLeagueStatistics);
      tools.Add(ToolRegistry.Match.GetLeagueTable);
    }

    return serviceProvider.ResolveTools(tools.ToArray());
  }

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new DateProvider(),
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new WebSearchProvider(serviceProvider.GetRequiredService<ISearchService>()),
    new AgentModeProvider(),
    new TodoProvider(),
  ];
}

internal sealed class PaperBetFollowUpStep(int matchId) : IAgentPhaseStep
{
  public string BuildPrompt() => """
    Create a fictional prediction slip for this match as a research validation artifact.
    This is not a real bet and has no financial implications.

    The purpose is to test whether your prior research produces internally consistent and logically supported predictions when forced into explicit outcomes.
    Use only information derived from your prior research. Do not introduce new facts, assumptions, or external knowledge.
    Selections must be strictly consistent with your prior analysis.
    Do not consider odds, market pricing, or value. These are irrelevant for this task.

    Each selection should represent a distinct, logically independent implication of your prior research.

    Before finalizing the slip, validate that all selections are mutually consistent with each other and with the conclusions of your prior research.
    """;

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([
      ToolRegistry.ResearchBet.GetMatchBasicInfo(matchId),
      ToolRegistry.ResearchBet.GetMatchEvents(matchId),
      ToolRegistry.ResearchBet.PlaceBetSlip(matchId),
    ]);

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new DateProvider(),
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new WebSearchProvider(serviceProvider.GetRequiredService<ISearchService>()),
  ];
}
