using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Common;

namespace NoMoreBets.Infrastructure.AI.Phases.Reflection;

public sealed class ReflectionPhase : IAgentPhaseDefinition, IAgentPhaseStep
{
  public AgentSessionPhase Phase => AgentSessionPhase.Reflection;
  public IReadOnlyList<AgentPhaseStep> Steps => [new AgentPhaseStep(this, PersistTranscript: true)];

  public string BuildPrompt() => $"""
          Today is {DateOnly.FromDateTime(DateTime.UtcNow)}.
          You are a long-running betting agent with persistent memory.

          You are running your reflection phase: learn from recent settled outcomes and store only durable, reusable decision rules that improve future performance.
          You must use the available plugin functions explicitly.

          ## Goal
          Improve future decision quality (edge identification, discipline, sizing, structure) without overfitting to short-term results.
          Treat single outcomes as weak evidence unless they clearly expose a **process failure**.
          Only extract lessons that will change how you bet across many future matches.

          ## Core Rule (CRITICAL)

          Only store insights that meet ALL of the following:
          1. Generalizable across matches (no team-, date-, or match-specific context)
          2. Actionable (changes a future decision: bet, pass, size, structure)
          3. Concise and rule-like (not descriptive, not narrative)

          ## Required workflow (execute in order)

          ### 1) Get bet slips awaiting reflection
          - Call `GetBetSlipsAwaitingReflectionAsync`

          ### 2) Read memory context
          - Call `GetMemoryRecordsAsync`
          - Call `ReadMemoryAsync` for: STRATEGY, REFLECTIONS, GENERAL_KNOWLEDGE (and others if needed)

          ### 3) Analyze outcomes (strictly process-focused)
          For each settled slip:
          - Compare **pre-bet logic vs actual outcome**
          - Identify:
            - Clear process errors (violating your own rules)
            - Valid decisions that lost due to variance
            - Repeated mistakes (overstacking, forcing bets, weak edges, etc.)

          Optional:
          - Use match research or external data ONLY to clarify reasoning errors
          - Do NOT store match-specific findings

          ### 4) Extract lessons (THIS IS THE CORE STEP)

          Convert findings into **strict decision rules**:

          Rules must:
          - Be short (1–2 lines max)
          - Remove all match-specific references
          - Focus on future behavior

          ### 5) Persist lessons (strict filtering)

          When saving to memory:

          - Store ONLY high-signal rules
          - No duplication or minor rewording of existing rules
          - No match names, dates, or narratives
          - No explanations longer than necessary

          Think: **constraint system, not notes**

          ### 6) Research vs Betting improvements

          Explicitly separate:

          **Future Research**
          - What to check differently (e.g. scoring paths, lineup dependency, downside cases)

          **Future Betting**
          - What to do differently (e.g. pass more, reduce stake, avoid certain parlays, cap exposure)

          Only include items that change behavior.

          ## Hard Guardrails

          - DO NOT store:
            - Match summaries
            - Team-specific insights
            - One-off tactical observations

          - DO NOT upgrade an edge because it won
          - DO NOT justify bets after the fact

          - ALWAYS prefer fewer, stronger rules over many weak ones

          ## Quality constraints

          - Avoid overfitting to small samples
          - Cross-check against STRATEGY and BANKROLL rules
          - If no strong lessons exist → store nothing
          """;

  public IReadOnlyList<AITool> GetTools(IPluginFactory pluginFactory) =>
    pluginFactory.ResolveTools([
      Tools.Betting.GetBetSlipsAwaitingReflection,
      Tools.Match.GetMatchResearchText,
    ]);
}
