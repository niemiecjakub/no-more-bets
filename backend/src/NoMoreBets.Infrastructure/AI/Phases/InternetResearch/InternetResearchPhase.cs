using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.AgentTools;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.Memories;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Providers.AgentMode;
using NoMoreBets.Infrastructure.AI.Providers.Memories;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using NoMoreBets.Infrastructure.AI.Providers.Todo;
using NoMoreBets.Infrastructure.AI.Providers.WebSearch;
using NoMoreBets.Infrastructure.AI.Tools;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.AI.Phases.InternetResearch;

internal sealed class InternetResearchExecuteStep : IAgentPhaseStep
{
  public string AgentName => "InternetResearchAgent";

  public string AgentInstructions => """
    Role: Scouting analyst building reusable intelligence for later match-day research.

    Goal: Distill upcoming-fixture intelligence that changes how matches should be interpreted in the research phase.

    Success criteria:
    - Fixtures prioritized with brief rationale
    - Per-fixture insights are reusable synthesis, not article summaries
    - Existing memories listed and read before writing; no duplicate insights saved

    Constraints:
    - Store only insights that change match interpretation — raw news is not a memory
    - Fixture-scoped notes only, named with the match date; no club or league profile memories
    - Label uncertainty; separate structural signal from narrative noise
    - Cross-check material claims and state conflicts between sources
    - Skip stable, low-information fixtures unless new evidence could change the read

    Stop: Finish when high-value fixtures are covered or no new material insights remain.
    """;

  public string BuildPrompt() => """
      Scout upcoming fixtures and persist reusable intelligence for match-day research.
      """;

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider)
  {
    var matchTool = serviceProvider.GetRequiredService<MatchTool>();
    return [AiToolBind.Bind(matchTool.GetUpcomingMatchesAsync, AgentToolCatalog.Match.GetUpcomingMatches.Name)];
  }

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new MemoriesProvider(
      serviceProvider.GetRequiredService<IMemoryRepository>(),
      serviceProvider.GetRequiredService<IUnitOfWork>()),
    new WebSearchProvider(
      serviceProvider.GetRequiredService<ISearchService>(),
      serviceProvider.GetRequiredService<AgentRunToolMetadataCollector>()),
    new AgenticModeProvider(),
    new TodoListProvider(),
  ];
}
