using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.AgentTools;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Providers.AgentMode;
using NoMoreBets.Infrastructure.AI.Providers.Memories;
using NoMoreBets.Infrastructure.AI.Providers.Todo;
using NoMoreBets.Infrastructure.AI.Tools;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.AI.Phases.Reflection;

internal sealed class ReflectionExecuteStep : IAgentPhaseStep
{
  public string AgentName => "ReflectionAgent";

  public string AgentInstructions => """
    Role: Process auditor for the betting system.

    Goal: Improve future decision quality from settled slips — not explain individual outcomes.

    Success criteria:
    - Every slip awaiting reflection assessed in order: STRATEGY compliance at placement, locked reasoning quality, then structural error / discipline issue / variance
    - STRATEGY updated only with generalizable, behavior-changing rules backed by repeated or structural evidence
    - New rules cross-checked against STRATEGY and merged with equivalent rules
    - Research improvements separated from execution improvements (selection, sizing, structure)

    Constraints:
    - Single outcomes are weak evidence unless they reveal a repeated process error
    - Good strategy ignored → no new rule; bad strategy followed well → change strategy
    - Valid rules must be generalizable, behavior-changing, concise, and independently actionable
    - No match-specific rules, no rationalizing losses, no confidence upgrades from wins alone

    Stop: Complete when all slips are reviewed. Leave STRATEGY unchanged if no rule passes the bar — no changelog or "no new rule" stamps.
    """;

  public string BuildPrompt() => "Review all settled slips awaiting reflection.";

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider)
  {
    var betting = serviceProvider.GetRequiredService<BettingTool>();
    var matchTool = serviceProvider.GetRequiredService<MatchTool>();
    return
    [
      AiToolBind.Bind(betting.GetBetSlipsAwaitingReflectionAsync, AgentToolCatalog.Betting.GetBetSlipsAwaitingReflection.Name),
      AiToolBind.Bind(matchTool.GetMatchResearchTextAsync, AgentToolCatalog.Match.GetMatchResearchText.Name),
    ];
  }

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new AgenticModeProvider(),
    new TodoListProvider(),
  ];
}
