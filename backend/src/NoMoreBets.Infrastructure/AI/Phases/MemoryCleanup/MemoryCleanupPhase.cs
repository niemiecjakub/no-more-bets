using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Providers.AgentMode;
using NoMoreBets.Infrastructure.AI.Providers.Memories;
using NoMoreBets.Infrastructure.AI.Providers.Todo;
using NoMoreBets.Infrastructure.AI.Tools;

namespace NoMoreBets.Infrastructure.AI.Phases.MemoryCleanup;

public static class MemoryCleanupPhaseDefinition
{
  public static AgentSessionPhase Phase => AgentSessionPhase.MemoryCleanup;
}

internal sealed class MemoryCleanupExecuteStep : IAgentPhaseStep
{
  private const int DaysCutoff = 2;

  public string AgentName => "MemoryCleanupAgent";

  public string AgentInstructions => """
    Role: Records clerk for persistent agent memories.

    Goal: Remove obsolete fixture-specific content while preserving durable knowledge.

    Success criteria:
    - Every candidate memory reviewed against the retention rule
    - Stale fixture noise surgically removed, trimmed, merged, or deleted
    - STRATEGY, BANKROLL_MANAGEMENT, REFLECTIONS, and GENERAL_KNOWLEDGE remain intact

    Constraints:
    - Decide stale vs keep from name and description first; read full content only when needed
    - Prefer surgical edit over deleting a whole record
    - Never wholesale-delete cross-cutting process records
    - Delete an entire record only when the whole named record is obsolete fixture noise

    Stop: Finish when all candidates are reviewed and durable records are preserved.
    """;

  public string BuildPrompt()
  {
    var now = DateTime.UtcNow;
    var today = DateOnly.FromDateTime(now);
    var utcCutoff = now.AddDays(-DaysCutoff);

    return $"""
          Today (UTC): {today:yyyy-MM-dd}.

          Retention: fixture-specific content for kickoffs strictly before {utcCutoff:yyyy-MM-dd HH:mm} UTC (more than {DaysCutoff} days old) is outside retention for ephemeral notes (lineups, injury snapshots, single-fixture narratives, stale hype).

          Infer stale vs keep from memory name and description first. Inventory candidates, then clean.
          """;
  }

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([]);

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new AgenticModeProvider(),
    new TodoListProvider(),
  ];
}
