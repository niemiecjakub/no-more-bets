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
using NoMoreBets.Infrastructure.AI.Providers.WebSearch;
using NoMoreBets.Infrastructure.AI.Tools;

namespace NoMoreBets.Infrastructure.AI.Phases.InternetResearch;

public sealed class InternetResearchPhaseDefinition
{
  private InternetResearchPhaseDefinition()
  {
    Steps =
    [
      new AgentPhaseStep(new InternetResearchExecuteStep(), PersistTranscript: true),
    ];
  }

  public AgentSessionPhase Phase => AgentSessionPhase.InternetResearch;
  public IReadOnlyList<AgentPhaseStep> Steps { get; }

  public static InternetResearchPhaseDefinition Create()
    => new();

  private sealed class InternetResearchExecuteStep : IAgentPhaseStep
  {
    public string BuildPrompt() => """
          You are conducting research for upcoming matches for yourself, not for a syndicate or external audience.
          Focus on narratives, news, sentiment, and game context that your future self can reuse in later match-level analysis and betting decisions.
          Structure output so your future self can quickly reuse it in the betting phase.

          Goal:
          Produce one or more general research briefs for upcoming fixtures that your future self can use for later match-level analysis and betting decisions.

          Completion criteria:
          Key upcoming fixtures have been surveyed and prioritized fixtures researched.
          Distilled, reusable insights are persisted to memory — not raw copy-paste dumps.

          Break the work into todos at the start, then work through them marking items complete as you finish.

          Review memory for relevant context before gathering new material. Survey upcoming fixtures and identify which matches merit deeper internet research versus a quick pass. Confirm upcoming fixtures still align with expectations and adjust if reality differs materially.

          Gather internet context for prioritized fixtures — match and club news, league updates, sentiment, and related context. Prioritize recent, reliable sources and label uncertainty. Persist distilled, reusable insights to memory.

          ## Quality constraints
          - Be evidence-driven and explicit about missing data
          - Cross-check important claims when deeper validation is warranted
          """;

    public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
      serviceProvider.ResolveTools([
        ToolRegistry.Match.GetUpcomingMatches,
      ]);

    public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
    [
      new DateProvider(),
      new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
      new WebSearchProvider(serviceProvider.GetRequiredService<ISearchService>()),
      new AgentModeProvider(new AgentModeProviderOptions { DefaultMode = "execute" }),
      new TodoProvider(),
    ];
  }
}
