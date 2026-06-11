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

namespace NoMoreBets.Infrastructure.AI.Phases.Reflection;

public sealed class ReflectionPhaseDefinition
{
  private ReflectionPhaseDefinition()
  {
    Steps =
    [
      new AgentPhaseStep(new ReflectionExecuteStep(), PersistTranscript: true),
    ];
  }

  public AgentSessionPhase Phase => AgentSessionPhase.Reflection;
  public IReadOnlyList<AgentPhaseStep> Steps { get; }

  public static ReflectionPhaseDefinition Create()
    => new();

  private sealed class ReflectionExecuteStep : IAgentPhaseStep
  {
    public string BuildPrompt() => """
          Learn from recent settled outcomes and identify durable, reusable decision rules that could improve future performance.
          Treat single outcomes as weak evidence unless they clearly expose a process failure.
          Only extract insights that will change how you bet across many future matches.
          Improve future decision quality (edge identification, discipline, sizing, structure) without overfitting to short-term results.

          Goal:
          Extract and store only high-signal, generalizable decision rules from settled bet slips.

          Completion criteria:
          All settled bet slips awaiting reflection have been analyzed from a process perspective.
          High-signal rules are persisted to memory, or it is explicitly determined that no strong lessons exist and nothing is stored.

          Break the work into todos at the start, then work through them marking items complete as you finish.

          Core rule — only store insights that meet ALL of the following:
          - Generalizable across matches (no team-, date-, or match-specific context)
          - Actionable (changes a future decision: bet, pass, size, structure)
          - Concise and rule-like (not descriptive, not narrative)

          Identify settled bet slips awaiting reflection. Review memory for strategy, reflections, and general knowledge.

          For each settled slip in scope, analyze outcomes strictly from a process perspective: compare pre-bet logic versus actual outcome, separate clear process errors from valid decisions that lost due to variance, and note repeated mistakes such as overstacking, forcing bets, or weak edges.

          Convert findings into strict decision rules — short, match-agnostic, and focused on future behavior. Persist only high-signal rules to memory with no duplication or minor rewording of existing rules, no match names, dates, or narratives. Think constraint system, not notes.

          Explicitly separate future research improvements from future betting behavior changes where relevant. Only include items that change behavior.

          ## Quality constraints
          - Do not store match summaries, team-specific insights, or one-off tactical observations
          - Do not upgrade an edge because it won or justify bets after the fact
          - Cross-check against strategy and bankroll rules
          - Prefer fewer, stronger rules over many weak ones
          - If no strong lessons exist, store nothing
          """;

    public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
      serviceProvider.ResolveTools([
        ToolRegistry.Betting.GetBetSlipsAwaitingReflection,
        ToolRegistry.Match.GetMatchResearchText,
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
