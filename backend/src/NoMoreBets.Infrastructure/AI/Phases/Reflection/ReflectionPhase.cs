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

public static class ReflectionPhaseDefinition
{
  public static AgentSessionPhase Phase => AgentSessionPhase.Reflection;
}

internal sealed class ReflectionExecuteStep : IAgentPhaseStep
{
public string BuildPrompt() => """
    Learn from recent settled outcomes and extract only durable, reusable decision rules that improve future betting performance.
    
    Treat individual outcomes as weak evidence unless they reveal a repeated or structurally meaningful process error.
    
    Your goal is not to explain results, but to improve future decision quality across many future matches.
    
    Focus on improving:
    - edge detection quality
    - discipline in selection
    - bankroll and sizing behavior
    - structural decision consistency
    
    Do not optimize for explaining wins or losses.
    
    GOAL
    
    Extract and store only high-signal decision rules derived from systematic patterns across multiple outcomes.
    
    A valid rule must satisfy ALL of the following:
    - Generalizable across matches, teams, and contexts
    - Behavior-changing (it would alter a future decision, not just describe it)
    - Based on repeated patterns, structural errors, or consistent success/failure modes
    - Concise, rule-like, and independently actionable
    
    WORKFLOW
    
    Identify all settled bet slips awaiting review.
    
    For each slip:
    - Compare pre-bet reasoning vs actual outcome
    - Determine whether outcome reflects:
      (a) structural decision error
      (b) execution issue
      (c) variance/noise
    - Only proceed to rule extraction if a repeatable pattern or structural issue is present
    
    RULE EXTRACTION REQUIREMENTS
    
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
    
    If no high-signal, repeatable behavioral pattern is identified, store nothing.
    
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
    new WebSearchProvider(serviceProvider.GetRequiredService<ISearchService>()),
    new AgentModeProvider(),
    new TodoProvider(),
  ];
}
