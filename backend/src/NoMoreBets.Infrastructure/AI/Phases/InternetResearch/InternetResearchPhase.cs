using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Providers.AgentMode;
using NoMoreBets.Infrastructure.AI.Providers.Date;
using NoMoreBets.Infrastructure.AI.Providers.Memories;
using NoMoreBets.Infrastructure.AI.Providers.Todo;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using NoMoreBets.Infrastructure.AI.Providers.WebSearch;
using NoMoreBets.Infrastructure.AI.Tools;

namespace NoMoreBets.Infrastructure.AI.Phases.InternetResearch;

public static class InternetResearchPhaseDefinition
{
  public static AgentSessionPhase Phase => AgentSessionPhase.InternetResearch;
}

internal sealed class InternetResearchExecuteStep : IAgentPhaseStep
{
  public string BuildPrompt() => """
      This is your scouting round: stock the shelf that your future match-day research will cook from. Deep per-fixture analysis happens later, in the research phase — here you gather and distill what it will need.

      The purpose of this phase is to build reusable, decision-relevant intelligence about upcoming fixtures.
      Build structured understanding that your future self can directly reuse during match-level analysis and decision-making.

      Before writing anything, list your existing memory records and read the relevant ones. Do not re-save an insight that is already there.

      PRIORITY OBJECTIVE

      Transform raw information (news, context, sentiment, updates) into insights that change how a match should be interpreted.
      Do not store raw information unless it directly contributes to understanding match outcomes.

      WORKFLOW

      1. Survey upcoming fixtures
      Identify which matches are structurally worth deeper investigation based on potential relevance, uncertainty, or information value.

      2. Prioritize research effort
      Focus only on matches where new information could plausibly change match interpretation. Ignore low-information or stable fixtures.

      3. Deep context gathering (only for prioritized matches)
      Gather relevant external information such as news, squad updates, tactical commentary, and contextual signals.

      4. Synthesis into reusable insights
      Convert gathered information into:
      - changes in expected strength or style for this fixture
      - new uncertainties affecting match interpretation
      - context that materially affects outcome probability

      Do not preserve raw news unless it changes interpretation.

      CORE PRINCIPLE

      Information is only valuable if it changes how a match should be understood or evaluated later.

      Sentiment, narratives, and media coverage should only be included when they are likely to influence team performance, lineup decisions, or meaningful expectations about the match.

      UNCERTAINTY HANDLING

      Explicitly label uncertainty when information is incomplete, conflicting, or based on weak signals.

      OUTPUT REQUIREMENTS

      - Prioritized fixtures with reasoning for inclusion
      - Concise, reusable insights per fixture (not article summaries)
      - Clear separation between signal (structural insight) and noise (reporting, speculation)
      - Persist only time-bound, fixture-scoped notes when needed (named with the match date). Do not create or append club/league profile memories.

      QUALITY CONSTRAINTS

      - Be evidence-driven
      - Prefer synthesis over description
      - Cross-check important claims when necessary
      - Avoid over-weighting media narratives unless they affect expected match outcomes
      """;

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([
      ToolRegistry.Match.GetUpcomingMatches,
    ]);

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new DateProvider(),
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new WebSearchProvider(
      serviceProvider.GetRequiredService<ISearchService>(),
      serviceProvider.GetRequiredService<AgentRunToolMetadataCollector>()),
    new AgentModeProvider(),
    new TodoProvider(),
  ];
}
