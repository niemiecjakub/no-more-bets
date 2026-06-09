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

public sealed class BettingPhaseDefinition : IAgentPhaseDefinition
{
  private BettingPhaseDefinition(bool includeXPostFollowUp)
  {
    var steps = new List<AgentPhaseStep>
    {
      new(new BettingExecuteStep(), PersistTranscript: true),
    };
    if (includeXPostFollowUp)
    {
      steps.Add(new AgentPhaseStep(new XPostFollowUpStep(), PersistTranscript: false));
    }

    Steps = steps;
  }

  public AgentSessionPhase Phase => AgentSessionPhase.Betting;
  public IReadOnlyList<AgentPhaseStep> Steps { get; }

  public static BettingPhaseDefinition Create(bool includeXPostFollowUp)
    => new(includeXPostFollowUp);

  private sealed class BettingExecuteStep : IAgentPhaseStep
  {
    public string BuildPrompt() => """
          Review every match open for betting and align with stored strategy and bankroll rules.
          You may place zero bet slips (pass entirely), exactly one bet slip, or more than one bet slip in this run, as strategy and bankroll allow.
          Each bet slip is either a single (one selection on one market) or a parlay (multiple selections on the same slip; selections may span different matches).

          Goal:
          Place value-based, strategy-aligned bets while maintaining sensible bankroll protection, but avoid overly strict filtering that prevents reasonable betting activity.

          Completion criteria:
          Every open match has been reviewed and given an explicit pass-or-bet decision.
          All qualifying opportunities have been acted on — zero slips placed is a valid outcome when nothing qualifies.
          Distilled learnings from this run are persisted to memory.

          Break the work into todos at the start, then work through them marking items complete as you finish.

          Begin by reviewing memory for strategy, bankroll rules, reflections, and any match-specific insights. Assess current exposure against pending positions to avoid duplicate or unjustified redundant exposure on the same outcomes. Survey open betting opportunities and identify which fixtures merit serious consideration versus a quick pass.

          For each match that warrants serious consideration, build a full decision picture using stored match analysis, current prices, and any late-breaking information that could change the thesis. Fetch exotic market prices only when you intend a Handicap or ExactScore selection.

          Evaluate each candidate selection against value versus current prices, alignment with strategy and bankroll management, confidence and invalidation triggers, and overlap with pending slips. Do not add redundant positions on the same outcome unless clearly justified.

          If nothing qualifies, place no slips and summarize the pass in analyst terms. If one or more opportunities qualify, place one slip per distinct bet with appropriate stake and selections. Persist distilled learnings to memory — concise insights and takeaways, not raw data dumps.

          ## Quality constraints
          - Do not skip memory, balance checks, analysis, or current prices for matches you seriously consider
          - Cross-check important claims when deeper validation is warranted
          - If data is missing, state it explicitly and continue with best-effort reasoning
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
      new AgentModeProvider(new AgentModeProviderOptions { DefaultMode = "execute" }),
      new TodoProvider(),
    ];
  }

  private sealed class XPostFollowUpStep : IAgentPhaseStep
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
}
