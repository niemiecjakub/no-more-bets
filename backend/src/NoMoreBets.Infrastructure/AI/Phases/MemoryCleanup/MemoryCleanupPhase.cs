using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Providers.AgentMode;
using NoMoreBets.Infrastructure.AI.Providers.Date;
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

  public string BuildPrompt()
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var utcCutoff = DateTime.UtcNow.AddDays(-DaysCutoff);

    return $"""
          You are running a maintenance pass: review saved memories and remove content that will no longer be useful.
          Today is {today} (UTC calendar date).

          Retention rule for match-specific material:
          - Fixture or match-specific content whose **match date / kickoff (interpret as UTC unless clearly local)** was **more than {DaysCutoff} days before today** is outside retention for ephemeral notes (e.g. lineups, injury snapshots, narrow "this fixture" narratives, stale pre-match hype, post-mortems that only mattered for that one game).
          - Cutoff instant for comparisons: strictly before {utcCutoff:yyyy-MM-dd HH:mm} UTC.
          - **Primary signal:** names usually embed the fixture or date; each listing row includes a **description** when present. Decide stale vs keep from name and description first; read full content only when that is not enough to judge safely. Match IDs and club names in the body are secondary cues.

          **Preserve** durable knowledge unless it is purely redundant with discarded fixture noise:
          - STRATEGY, BANKROLL_MANAGEMENT, REFLECTIONS, GENERAL_KNOWLEDGE, and other cross-cutting process lessons should stay; only remove or shorten passages that are exclusively about old fixtures and no longer aid future research or betting.

          Goal:
          Safely trim or remove obsolete match-specific memory content while preserving durable knowledge.

          Completion criteria:
          All candidate memories have been reviewed against the retention rule.
          Stale fixture-specific content has been surgically removed, trimmed, merged, or deleted as appropriate.
          Durable strategy, bankroll, reflection, and general knowledge records remain intact.

          Break the work into todos at the start, then work through them marking items complete as you finish.

          Inventory saved memories and identify records that may hold match-specific or time-bound content. For each candidate, infer from name and description whether cleanup is warranted before reading full content.

          For records that need cleanup, read full content only when name and description are not enough to decide safely. Prefer surgical removals over wiping entire records when only part of the content is obsolete. Replace entire bodies when that is clearer and safe. Merge related memories into one distilled record when appropriate, then remove redundant sources. Delete entire records only when the whole named record is obsolete — never wholesale-delete durable strategy or bankroll records by mistake.

          ## Quality constraints
          - Do not remove or wipe durable strategy, bankroll, or calibration lessons unless they are clearly obsolete duplicate fixture chatter
          """;
  }

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([]);

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new DateProvider(),
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new AgentModeProvider(new AgentModeProviderOptions { DefaultMode = "execute" }),
    new TodoProvider(),
  ];
}
