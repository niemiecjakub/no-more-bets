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
    new AgentModeProvider(new AgentModeProviderOptions { DefaultMode = "execute" }),
    new TodoProvider(),
  ];
}

internal sealed class PaperBetFollowUpStep(int matchId) : IAgentPhaseStep
{
   public string BuildPrompt() => """
          Goal:
          Create a paper (fictional) prediction slip for this match as a research artifact that tests the quality of your prior research.

          Completion criteria:
          A paper bet slip is placed with valid, non-contradictory selections based strictly on your prior research.
          Selections maximize correctness of predictions — odds are unavailable and must be ignored entirely.

          This is not a real bet and does not affect bankroll in any way.
          Single selections are acceptable but multiple selections (parlays) are preferred.
          Do not include contradictory or overlapping selections.
          Avoid combining markets that express the same dimension in conflicting ways.
          You cannot select multiple options from the same market.

          Confirm match and club context from your prior research, review available markets and outcome options for this fixture, then place the paper slip.
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
