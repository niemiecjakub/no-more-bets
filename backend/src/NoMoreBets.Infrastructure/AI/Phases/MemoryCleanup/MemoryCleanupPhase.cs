using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Common;

namespace NoMoreBets.Infrastructure.AI.Phases.MemoryCleanup;

public sealed class MemoryCleanupPhase : IAgentPhaseDefinition, IAgentPhaseStep
{
  private const int DaysCutoff = 2;

  public AgentSessionPhase Phase => AgentSessionPhase.MemoryCleanup;
  public IReadOnlyList<AgentPhaseStep> Steps => [new AgentPhaseStep(this, PersistTranscript: true)];

  public string BuildPrompt()
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var utcCutoff = DateTime.UtcNow.AddDays(-DaysCutoff);

    return $"""
          Today is {today} (UTC calendar date).
          You are a long-running betting agent with persistent memory.

          You are running a maintenance pass: review saved memories and remove or trim content that will no longer be useful.

          Retention rule for match-specific material:
          - Fixture or match-specific content whose **match date / kickoff (interpret as UTC unless clearly local)** was **more than {DaysCutoff} days before today** is outside retention for ephemeral notes (e.g. lineups, injury snapshots, narrow “this fixture” narratives, stale pre-match hype, post-mortems that only mattered for that one game).
          - Cutoff instant for comparisons: strictly before {utcCutoff:yyyy-MM-dd HH:mm} UTC.
          - **Primary signal:** names usually embed the fixture or date; each listing row includes a **description** when present. Decide stale vs keep from name and description first; call `ReadMemoryAsync` only when that is not enough to judge safely. Match IDs and club names in the body (after you read) are secondary cues.

          **Preserve** durable knowledge unless it is purely redundant with discarded fixture noise:
          - STRATEGY, BANKROLL_MANAGEMENT, REFLECTIONS, GENERAL_KNOWLEDGE, and other cross-cutting process lessons should stay; only remove or shorten passages that are exclusively about old fixtures and no longer aid future research or betting.

          You must use the available plugin functions explicitly for reads, edits, and deletes as needed.

          ## Required workflow (execute in order)

          1) Inventory:
          - Call `GetMemoryRecordsAsync`.

          2) For each record from the inventory, infer from **name** and **description** whether it may hold match-specific or time-bound content.
          - Call `ReadMemoryAsync` only when name and description are not enough to decide—then use the full body before editing or deleting.

          3) Cleanup:
          - Prefer `ReplaceMemoryAsync` for surgical removals (verbatim `oldText` from the read output; `newText` empty removes the span).
          - Use `WriteMemoryAsync` when replacing the entire body is clearer and safe (still keeps the same record name).
          - You may **merge** several related memories into one: create or overwrite a target record with `WriteMemoryAsync` (distilled combined content), then trim or `DeleteMemoryAsync` the redundant source records when the merge is complete.
          - Use `DeleteMemoryAsync` when the **entire named record is obsolete**; same naming rules as other memory tools. Do not use it for durable records listed above.

          4) Finish with a short summary.

          ## Guardrails
          - Do not remove or wipe durable strategy, bankroll, or calibration lessons unless they are clearly obsolete duplicate fixture chatter; never `DeleteMemoryAsync` those wholesale by mistake.
          """;
  }

  public IReadOnlyList<AITool> GetTools(IPluginFactory pluginFactory) =>
    MemoryCleanupPhaseTools.CreateStepTools(pluginFactory);
}
