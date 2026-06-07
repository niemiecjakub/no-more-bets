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
using NoMoreBets.Infrastructure.AI.Providers.WebSearch;
using NoMoreBets.Infrastructure.AI.Tools;

namespace NoMoreBets.Infrastructure.AI.Phases.Test;

public sealed class TestPhaseDefinition : IAgentPhaseDefinition
{
  private TestPhaseDefinition(Match match)
  {
    Steps =
    [
      new AgentPhaseStep(new TestPlanStep(match), PersistTranscript: true),
      new AgentPhaseStep(new TestExecuteStep(match), PersistTranscript: true),
    ];
  }

  public AgentSessionPhase Phase => AgentSessionPhase.Test;
  public IReadOnlyList<AgentPhaseStep> Steps { get; }

  public static TestPhaseDefinition ForMatch(Match match)
    => new(match);

  private static IReadOnlyList<AITool> GetMatchResearchTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([
      ToolRegistry.Match.GetLineups,
      ToolRegistry.Match.GetInjuries,
      ToolRegistry.Match.GetHead2HeadStats,
      ToolRegistry.Match.GetClubDailySummary,
      ToolRegistry.Match.GetClubRecentGames,
      ToolRegistry.Match.GetClubLeagueStatistics,
      ToolRegistry.Match.GetLeagueTable,
      ToolRegistry.Match.GetMatchBettingOddsHistory,
      ToolRegistry.Match.GetClubRollingPerformance,
      ToolRegistry.Match.SaveMatchAnalysis,
    ]);

  private static IReadOnlyList<AIContextProvider> GetPlanContextProviders(IServiceProvider serviceProvider) =>
  [
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new WebSearchProvider(serviceProvider.GetRequiredService<ISearchService>()),
    new AgentModeProvider(new AgentModeProviderOptions { DefaultMode = "plan" }),
    new TodoProvider(),
  ];

  private static IReadOnlyList<AIContextProvider> GetExecuteContextProviders(IServiceProvider serviceProvider) =>
  [
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new WebSearchProvider(serviceProvider.GetRequiredService<ISearchService>()),
    new AgentModeProvider(new AgentModeProviderOptions { DefaultMode = "execute" }),
    new TodoProvider(),
  ];

  private sealed class TestPlanStep(Match match) : IAgentPhaseStep
  {
    public string BuildPrompt() => $"""
          Today is {DateOnly.FromDateTime(DateTime.UtcNow)}.
          You are a long-running betting agent with persistent memory.

          You are now in **plan mode**. Your task is to build a research plan for this match — not to produce final research yet:
          - Match ID: {match.Id}
          - Fixture: {match.HomeClub.Name} (ID: {match.HomeClub.Id}) vs {match.AwayClub.Name} (ID: {match.AwayClub.Id})
          - Kickoff (UTC): {match.MatchDate:yyyy-MM-dd HH:mm}

          Important context:
          You are NOT reacting directly to live betting market movements or line shifts during this research phase.
          Because of this, you should assume you do NOT have a timing-based market edge (no late line movement advantage, no sharp market reaction signals).
          Your edge must come only from structural, statistical, tactical, or contextual analysis—not from market positioning or timing.

          Goal:
          Create a decision-oriented research plan that your execute-mode self will follow to produce complete match research for your own later betting decision.

          ## Outline

          1) Review what you already know from memory before planning anything new.

          2) Optionally gather a lightweight snapshot of the fixture — lineups, injuries, odds, league standing — only if it helps shape the plan.

          3) Break the research work into a clear todo list of trackable items.

          4) Persist the plan to memory so it survives beyond this session.
          - Keep it concise, structured, and directly useful for execution
          - Cover which data to gather, key questions to answer, and betting angles to evaluate

          5) Summarize the plan briefly in your response.

          6) Before finishing, switch to execute mode so the next step can run autonomously.

          ## Quality constraints
          - Be analytical and evidence-driven in scoping the plan
          - If data is missing during exploration, note it in the plan and continue
          - Focus on what execution needs to deliver, not on describing your own process
          """;

    public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
      GetMatchResearchTools(serviceProvider);

    public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
      GetPlanContextProviders(serviceProvider);
  }

  private sealed class TestExecuteStep(Match match) : IAgentPhaseStep
  {
    public string BuildPrompt() => $"""
          Today is {DateOnly.FromDateTime(DateTime.UtcNow)}.
          You are a long-running betting agent with persistent memory.

          You are now in **execute mode**. Execute the research plan you created in the prior step for this match:
          - Match ID: {match.Id}
          - Fixture: {match.HomeClub.Name} (ID: {match.HomeClub.Id}) vs {match.AwayClub.Name} (ID: {match.AwayClub.Id})
          - Kickoff (UTC): {match.MatchDate:yyyy-MM-dd HH:mm}

          Important context:
          You are NOT reacting directly to live betting market movements or line shifts during this research phase.
          Because of this, you should assume you do NOT have a timing-based market edge (no late line movement advantage, no sharp market reaction signals).
          Your edge must come only from structural, statistical, tactical, or contextual analysis—not from market positioning or timing.

          Goal:
          Create complete, decision-oriented research for this specific match that you will later use in your own betting phase.
          This is your personal prep work: your future self in the betting phase should be able to read this and decide whether to bet or pass.

          Work through the plan and todos from the prior step. Mark items complete as you finish them.

          ## Outline

          1) Review memory for relevant context before starting new analysis.

          2) Build core match intelligence: lineups, injuries, head-to-head history, odds history, and league table.

          3) Build team-level context for both clubs: league statistics, recent form, rolling performance, and daily summaries.

          4) Gather news and sentiment for both clubs where needed.
          - Separate meaningful signals from noise and assess source reliability
          - Cross-check important claims when deeper validation is warranted

          5) Synthesize a decision-oriented view with clear betting implications, potential value angles, and confidence drivers.

          6) Persist distilled learnings to memory — concise insights, patterns, and hypotheses, not raw data dumps.

          7) Produce a brief, scannable final report (under 500 words) and save it as the official match analysis for this fixture.
          Do not finish until the analysis is saved.

          8) Close with a short summary of key insights and betting implications.

          ## Quality constraints
          - Be analytical and evidence-driven
          - Cross-check important claims
          - If data is missing, state it explicitly and continue with best-effort reasoning
          - Do not skip required steps

          ### Guardrails
          - Focus on delivering the research output as if for a human analyst, not on describing your own process.
          """;

    public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
      GetMatchResearchTools(serviceProvider);

    public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
      GetExecuteContextProviders(serviceProvider);
  }
}
