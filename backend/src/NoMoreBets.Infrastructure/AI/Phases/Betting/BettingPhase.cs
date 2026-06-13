using MediatR;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Providers.AgentMode;
using NoMoreBets.Infrastructure.AI.Providers.Bankroll;
using NoMoreBets.Infrastructure.AI.Providers.Date;
using NoMoreBets.Infrastructure.AI.Providers.Memories;
using NoMoreBets.Infrastructure.AI.Providers.Todo;
using NoMoreBets.Infrastructure.AI.Providers.WebSearch;
using NoMoreBets.Infrastructure.AI.Tools;

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
  public string BuildPrompt() => """
    You are operating in the betting execution phase of a research-driven betting system.
    Your stored memory defines the default decision framework and constraints for all actions.
    Decide whether to place bets, and if so, construct bet slips consistent with memory-based strategy, bankroll constraints, and prior research.
    """;

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([
      ToolRegistry.Betting.GetAvailableMatches,
      ToolRegistry.Betting.GetCurrentOdds,
      ToolRegistry.Betting.GetMatchAnalysis,
      ToolRegistry.Betting.PlaceBetSlip,
      ToolRegistry.Betting.GetBetSlips,
    ]);

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new DateProvider(),
    new BankrollProvider(serviceProvider.GetRequiredService<IMediator>()),
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new WebSearchProvider(serviceProvider.GetRequiredService<ISearchService>()),
    new AgentModeProvider(),
    new TodoProvider(),
  ];
}

internal sealed class XPostFollowUpStep : IAgentPhaseStep
{
  public string BuildPrompt() => """
      Goal:
      Publish a concise X post summarizing the bets placed in the prior betting run.

      Completion criteria:
      A post is published via the X tool when bets were placed in the prior step.
      If no bets were placed, no post is needed.

      When posting, keep the tone professional yet engaging.
      Summarize the bets placed clearly and concisely.
      Always include hashtags for the league involved, derived from that league's name (e.g. Premier League as #PremierLeague, Serie A as #SerieA).
      """;

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([ToolRegistry.SocialMedia.CreateXPost]);

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new DateProvider(),
    new BankrollProvider(serviceProvider.GetRequiredService<IMediator>()),
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new WebSearchProvider(serviceProvider.GetRequiredService<ISearchService>()),
  ];
}
