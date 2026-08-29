using MediatR;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Providers.AgentMode;
using NoMoreBets.Infrastructure.AI.Providers.Bankroll;
using NoMoreBets.Infrastructure.AI.Providers.Betting;
using NoMoreBets.Infrastructure.AI.Providers.Memories;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using NoMoreBets.Infrastructure.AI.Providers.Todo;
using NoMoreBets.Infrastructure.AI.Providers.WebSearch;
using NoMoreBets.Infrastructure.AI.Tools;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.AI.Phases.Betting;

public sealed class BettingPhaseDefinition
{
  private BettingPhaseDefinition(bool includeXPostFollowUp)
  {
    IncludeXPostFollowUp = includeXPostFollowUp;
  }

  public AgentSessionPhase Phase => AgentSessionPhase.Betting;
  public bool IncludeXPostFollowUp { get; }

  public static BettingPhaseDefinition Create(bool includeXPostFollowUp)
    => new(includeXPostFollowUp);
}

internal sealed class BettingExecuteStep : IAgentPhaseStep
{
  string IAgentPhaseStep.AgentInstructions => $"""
    {AgentInstructions.ChandlerSoul}

    Goal: Grow the bankroll over time through active, disciplined betting.

    Success criteria:
    - Balance and existing exposure checked before staking
    - STRATEGY read;
    - Every placed bet has an honest win probability and brief rationale
    - If no bet meets the bar, explain why

    Constraints:
    - Never stake on an unresearched match
    - Size stakes to confidence
    - Note any STRATEGY deviation in the bet rationale

    Stop: Finish after evaluating the full slate and betting every positive-edge opportunity that fits STRATEGY and current exposure. Do not default to inactivity because evidence is imperfect.
    """;

  public string BuildPrompt() => "The betting window is open. Evaluate current opportunities.";

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([]);

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new BankrollProvider(serviceProvider.GetRequiredService<IMediator>()),
    new BettingProvider(serviceProvider.GetRequiredService<BettingTool>()),
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new WebSearchProvider(
      serviceProvider.GetRequiredService<ISearchService>(),
      serviceProvider.GetRequiredService<AgentRunToolMetadataCollector>()),
    new AgenticModeProvider(),
    new TodoListProvider(),
  ];
}

internal sealed class XPostFollowUpStep : IAgentPhaseStep
{
  string IAgentPhaseStep.AgentInstructions => AgentInstructions.ChandlerSoul;

  public string BuildPrompt() => """
      Publish an X post for bets placed in the prior step. Skip if no bets were placed.

      Lead with the read behind the bet, then selection, price, and stake. Include league hashtags (e.g. #PremierLeague).
      No outcome promises, no hype, no exclamation marks.
      """;

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([ToolRegistry.SocialMedia.CreateXPost]);
}
