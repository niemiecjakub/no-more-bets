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

namespace NoMoreBets.Infrastructure.AI.Phases.Reflection;

public static class ReflectionPhaseDefinition
{
  public static AgentSessionPhase Phase => AgentSessionPhase.Reflection;
}

internal sealed class ReflectionExecuteStep : IAgentPhaseStep
{
  public string BuildPrompt() => """
    This is your review session. Your own money settled since the last one; sit down with the numbers and be honest about what they say. Nobody audits you except your own ledger.
    
    Your goal is not to explain results, but to improve future decision quality across many future matches.
    
    Treat individual outcomes as weak evidence unless they reveal a repeated or structurally meaningful process error.
    
    Focus on improving:
    - edge detection quality
    - discipline in selection
    - bankroll and sizing behavior
    - structural decision consistency
    
    Do not optimize for explaining wins or losses.
    
    WORKFLOW
    
    1. Read your STRATEGY memory record.
    
    2. Review each settled slip awaiting reflection. Each slip carries the rationale and estimated win probability you locked at placement. For each one ask, in order:
       - Compliance: did this bet follow the strategy as written at the time? The rationale should say so; deviations without a stated reason are discipline failures regardless of outcome.
       - Judgment: was the locked reasoning sound given what was knowable then?
       - Outcome class: (a) structural decision error, (b) execution/discipline issue, or (c) variance/noise.
       Only proceed to rule extraction if a repeatable pattern or structural issue is present.
    
    Distinguish sharply: a bad strategy followed well needs a strategy change; a good strategy ignored needs nothing new written — the rule already existed and you broke it. Do not write a new rule to compensate for not following an old one.
    
    RULE EXTRACTION REQUIREMENTS
    
    A valid rule must satisfy ALL of the following:
    - Generalizable across matches, teams, and contexts
    - Behavior-changing (it would alter a future decision, not just describe it)
    - Based on repeated patterns, structural errors, or consistent success/failure modes
    - Concise, rule-like, and independently actionable
    
    When forming rules:
    - Prefer patterns observed across multiple decisions over single-instance insights
    - Avoid emotional or result-driven interpretation
    - Do not derive rules from winning outcomes alone unless supported by repeated evidence
    - Merge semantically similar rules into a single stronger rule
    - Reject vague behavioral advice (e.g. "be more disciplined", "trust model more")
    
    Explicitly separate:
    - research improvements (information quality, analysis improvements)
    - execution improvements (bet selection, sizing, structure)
    
    PERSISTENCE RULES
    
    Store only unique, non-overlapping rules.
    If a rule is already represented in memory, do not re-store it in modified form.
    
    If no high-signal, repeatable behavioral pattern is identified, store nothing beyond the strategy changelog line.
    
    QUALITY CONSTRAINTS
    
    - Do not store match-specific insights or one-off observations
    - Do not rationalize losing bets after the fact
    - Do not upgrade confidence because a bet won
    - Prefer fewer, higher-confidence rules over many weak ones
    - Cross-check against existing strategy before storing
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
    new WebSearchProvider(
      serviceProvider.GetRequiredService<ISearchService>(),
      serviceProvider.GetRequiredService<AgentRunToolMetadataCollector>()),
    new AgentModeProvider(),
    new TodoProvider(),
  ];
}
