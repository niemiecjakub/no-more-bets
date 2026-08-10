using MediatR;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Providers.AgentMode;
using NoMoreBets.Infrastructure.AI.Providers.Bankroll;
using NoMoreBets.Infrastructure.AI.Providers.Betting;
using NoMoreBets.Infrastructure.AI.Providers.Date;
using NoMoreBets.Infrastructure.AI.Providers.Memories;
using NoMoreBets.Infrastructure.AI.Providers.Todo;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
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
  public string BuildPrompt() => """
        Your betting window is open. This is your money on the line, so work it like the job it is.

        Before placing anything:
        1. Check your balance and existing exposure. You bet from what you have, not from what you hope to win back.
        2. Read your STRATEGY memory record. It is the set of rules your past reviews earned; you wrote it, you follow it. If it does not exist yet, create a short initial version (selection criteria, sizing rules, markets you avoid) before betting.

        Then evaluate current opportunities and act. Follow your written strategy when deciding. If a bet breaks a written rule, say so briefly in the rationale in natural voice — your review session will check, and an unexplained deviation is a discipline failure regardless of outcome.

        Set the estimated win probability honestly. It is scored against reality over time; flattering numbers only make you look worse later.

        You work with imperfect, delayed information — so does everyone at the counter. Certainty is not the bar; a defensible read at an acceptable price is. Protection comes from sizing the stake to your confidence, not from refusing to bet: small stake on a thinner read, larger on a stronger one. Parlays are a legitimate tool when the legs genuinely reinforce each other — size them for what they are.

        Passing on a match you cannot justify is fine. Passing on the entire window should be the exception and needs the same justification as a bet — idle capital earns nothing, and only settled slips teach you anything.
        """;

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([]);

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new DateProvider(),
    new BankrollProvider(serviceProvider.GetRequiredService<IMediator>()),
    new BettingProvider(serviceProvider.GetRequiredService<BettingTool>()),
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new WebSearchProvider(
      serviceProvider.GetRequiredService<ISearchService>(),
      serviceProvider.GetRequiredService<AgentRunToolMetadataCollector>()),
    new AgentModeProvider(),
    new TodoProvider(),
  ];
}

internal sealed class XPostFollowUpStep : IAgentPhaseStep
{
  public string BuildPrompt() => """
      Goal:
      Publish an X post about the bets placed in the prior betting run.

      Completion criteria:
      A post is published via the X tool when bets were placed in the prior step.
      If no bets were placed, no post is needed.

      This is your public record, in your own voice — dry, compressed, no hype. The reasoning is the product: lead with the actual read behind the bet (one or two sentences of the edge you saw), then the selection, price, and stake. State it like someone putting their own money where their analysis is, because you are.

      Never promise outcomes, never tout, never use exclamation marks. A slip that might lose is posted with the same tone as one that might win — it stays on the record either way.

      Always include hashtags for the league involved, derived from that league's name (e.g. Premier League as #PremierLeague, Serie A as #SerieA).
      """;

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([ToolRegistry.SocialMedia.CreateXPost]);

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new DateProvider(),
    new BankrollProvider(serviceProvider.GetRequiredService<IMediator>()),
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new WebSearchProvider(
      serviceProvider.GetRequiredService<ISearchService>(),
      serviceProvider.GetRequiredService<AgentRunToolMetadataCollector>()),
  ];
}
