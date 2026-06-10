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

public sealed class ResearchPhaseDefinition : IAgentPhaseDefinition
{
  private ResearchPhaseDefinition(Match match)
  {
    Steps =
    [
      new AgentPhaseStep(new ResearchExecuteStep(match), PersistTranscript: true),
      new AgentPhaseStep(new PaperBetFollowUpStep(match.Id), PersistTranscript: false),
    ];
  }

  public AgentSessionPhase Phase => AgentSessionPhase.Research;
  public IReadOnlyList<AgentPhaseStep> Steps { get; }

  public static ResearchPhaseDefinition ForMatch(Match match)
    => new(match);

  private sealed class ResearchExecuteStep(Match match) : IAgentPhaseStep
  {
    public string BuildPrompt() => $"""
          Match ID: {match.Id}
          Fixture: {match.HomeClub.Name} (ID: {match.HomeClub.Id}) vs {match.AwayClub.Name} (ID: {match.AwayClub.Id})
          Kickoff (UTC): {match.MatchDate:yyyy-MM-dd HH:mm}

          Important context:
          You are NOT reacting directly to live betting market movements or line shifts during this research phase.
          Because of this, you should assume you do NOT have a timing-based market edge (no late line movement advantage, no sharp market reaction signals).
          Your edge must come only from structural, statistical, tactical, or contextual analysis—not from market positioning or timing.

          Goal:
          Create complete, decision-oriented research for this specific match that you will later use in your own betting phase.
          This is your personal prep work: your future self in the betting phase should be able to read this and decide whether to bet or pass.

          Completion criteria:
          Core match intelligence and team-level context have been gathered and synthesized.
          Distilled learnings are persisted to memory.
          A brief, scannable final report (under 500 words) is saved as the official match analysis for this fixture via SaveMatchAnalysis.
          Do not finish until the analysis is saved.

          Break the work into todos at the start, then work through them marking items complete as you finish.

          Review memory for relevant context before starting new analysis. {(match.IsFifaWorldCup
            ? "Build core match intelligence covering lineups, injuries, head-to-head history, and odds history. Build team-level context for both national teams including recent form and rolling performance."
            : "Build core match intelligence covering lineups, injuries, head-to-head history, odds history, and league table. Build team-level context for both clubs including league statistics, recent form, rolling performance, and daily summaries.")}

          Gather news and sentiment for both clubs where needed, separating meaningful signals from noise and assessing source reliability. Cross-check important claims when deeper validation is warranted.

          Synthesize a decision-oriented view with clear betting implications, potential value angles, and confidence drivers. Persist distilled learnings to memory — concise insights, patterns, and hypotheses, not raw data dumps.

          ## Quality constraints
          - Be analytical and evidence-driven
          - Cross-check important claims
          - If data is missing, state it explicitly and continue with best-effort reasoning
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
        ToolRegistry.Match.SaveMatchAnalysis,
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

  private sealed class PaperBetFollowUpStep(int matchId) : IAgentPhaseStep
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
}
