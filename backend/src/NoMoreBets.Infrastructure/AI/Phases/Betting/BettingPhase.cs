using System.Globalization;
using MediatR;
using Microsoft.Extensions.AI;
using NoMoreBets.Application.Bankroll.GetDaysUntilPayday;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Common;

namespace NoMoreBets.Infrastructure.AI.Phases.Betting;

public sealed record BettingPhaseContext(string BalanceText, int DaysUntilPayday);

public sealed class BettingPhaseDefinition : IAgentPhaseDefinition
{
  private BettingPhaseDefinition(BettingPhaseContext context, bool includeXPostFollowUp)
  {
    var steps = new List<AgentPhaseStep>
    {
      new(new BettingPrimaryStep(context), PersistTranscript: true),
    };
    if (includeXPostFollowUp)
    {
      steps.Add(new AgentPhaseStep(new XPostFollowUpStep(), PersistTranscript: false));
    }

    Steps = steps;
  }

  public AgentSessionPhase Phase => AgentSessionPhase.Betting;
  public IReadOnlyList<AgentPhaseStep> Steps { get; }

  public static async Task<BettingPhaseDefinition> CreateAsync(
    IUnitOfWork unitOfWork,
    IMediator mediator,
    IPluginFactory pluginFactory,
    bool includeXPostFollowUp,
    CancellationToken cancellationToken)
  {
    var context = await LoadContextAsync(unitOfWork, mediator, cancellationToken).ConfigureAwait(false);
    return new BettingPhaseDefinition(context, includeXPostFollowUp);
  }

  private static async Task<BettingPhaseContext> LoadContextAsync(
    IUnitOfWork unitOfWork,
    IMediator mediator,
    CancellationToken cancellationToken)
  {
    var currentBalance = await unitOfWork.Bankroll
      .GetCurrentBalanceAsync(cancellationToken)
      .ConfigureAwait(false);
    var daysUntilPayday = await mediator
      .Send(new GetDaysUntilPaydayQuery(), cancellationToken)
      .ConfigureAwait(false);

    var balanceText = currentBalance.ToString("F2", CultureInfo.InvariantCulture);
    return new BettingPhaseContext(balanceText, daysUntilPayday);
  }

  private sealed class BettingPrimaryStep(BettingPhaseContext context) : IAgentPhaseStep
  {
    public string BuildPrompt() => $"""
          Today is {DateOnly.FromDateTime(DateTime.UtcNow)}.
          You are a long-running betting agent with persistent memory.
          Current account balance: {context.BalanceText}
          Days until payday: {context.DaysUntilPayday}

          You are executing the betting phase for the portfolio: review every match that is open for betting, align with stored strategy and bankroll rules.
          You may place zero bet slips (pass entirely), exactly one bet slip, or more than one bet slip in this run, as strategy and bankroll allow.
          Each call to `PlaceBetSlip` is one separate bet (one slip) with its own stake. That slip is either a single (one selection, one event market) or a parlay (multiple selections combined on the same slip; parlay selections can come from many different matches). The `betSelections` JSON array must contain at least one selection per slip: one element means a single bet; multiple elements mean a parlay on that slip.
          You must use the available plugin functions explicitly.

          Goal:
          Place value-based, strategy-aligned bets while maintaining sensible bankroll protection, but avoid overly strict filtering that prevents reasonable betting activity.

          ## Memory and research at any time
          You are not limited to the workflow steps below for memory or search. Whenever it helps your judgment, you may read or write any memory using `GetMemoryRecordsAsync`, `ReadMemoryAsync`, `WriteMemoryAsync`, `AppendMemoryAsync`, and `ReplaceMemoryAsync`. You may also call `SearchNewsAsync` and `GetWebGroundingAsync` (or any other search or grounding tools available to you) for whatever information you need, with queries you choose—not only for late-breaking match news.

          ## Required workflow (execute in order)

          1) Read memory context first:
          - Call `GetMemoryRecordsAsync`
          - Call `ReadMemoryAsync` to read relevant memories.

          2) Exposure:
          - Call `GetBetSlipsAsync` to see pending slips and avoid duplicate or unjustified redundant exposure on the same outcomes

          3) Enumerate opportunities:
          - Call `GetAvailableMatchesAsync`

          4) For each match you seriously consider (not every listed fixture), build a decision picture:
          - Relevant memories may include an insight for this match; use `ReadMemoryAsync` and factor that in alongside analysis and odds.
          - Call `GetMatchAnalysisAsync` for that match ID
          - Call `GetCurrentOddsAsync` only for matches you still consider. Do not fetch odds for matches you already rule out from analysis alone.
          - If and only if you intend a Handicap or ExactScore selection, call `GetCurrentOddsAsync` again for that match with `includeExoticMarkets` true before placing the slip.
          - If late-breaking information could materially change the thesis versus the stored analysis, use `SearchNewsAsync` and/or `GetWebGroundingAsync` with focused queries.
 
          5) Evaluate each candidate selection:
          - Value vs current prices (implied probability vs your view)
          - Alignment with STRATEGY, BANKROLL_MANAGEMENT, REFLECTIONS and GENERAL_KNOWLEDGE
          - Confidence and what would invalidate the view
          - Stake feasibility: stake must be > 0 and must not exceed your **remaining balance** for this run: the opening balance stated above minus the sum of all stake amounts from `PlaceBetSlip` calls you have already made in this same run; respect BANKROLL_MANAGEMENT (unit sizing, max stake, concentration)
          - Overlap with pending slips from `GetBetSlipsAsync`: do not add redundant positions on the same outcome unless clearly justified

          6) Decision:
          - If nothing qualifies: place no slips; summarize the pass in analyst terms (no tool dump)
          - If one or more opportunities qualify: place one slip per distinct bet you want (zero to many slips in total). For each slip, choose stake and build `betSelections`: one item for a single, several items for a parlay on that slip
          - Call `PlaceBetSlip` once per slip with valid JSON as described on the function. Never call `PlaceBetSlip` with an empty `betSelections` array

          7) Persist learnings:
          - Update durable insights with `AppendMemoryAsync`, `ReplaceMemoryAsync`, or `WriteMemoryAsync` as appropriate.
          - You may create new memories with `WriteMemoryAsync` if needed.
          - Store distilled takeaways, not raw tool output

          8) Finish with a short summary for a human: how many slips you placed (if any), singles vs parlays, key rationale, and main risks.

          ## Quality constraints
          - Do not skip memory, balance, analysis, or compact odds for matches you seriously consider

          ### Guardrails
          - In your final narrative to the user, do not mention internal process, tool names, or plugin mechanics.
          """;

    public IReadOnlyList<AITool> GetTools(IPluginFactory plugins) =>
      AgentToolFactory.CreateFromObject(plugins.CreateAgentBettingPlugin());
  }

  private sealed class XPostFollowUpStep : IAgentPhaseStep
  {
    public string BuildPrompt() => """
        If you placed any bets, publish a post on X - call CreateXPost with the post content.
        The post should be a concise summary of the bets you have just placed. 
        Keep the tone professional yet engaging. 
        Always include hashtags for the league involved, derived from that league's name (e.g. Premier League as #PremierLeague, Serie A as #SerieA).
        """;

    public IReadOnlyList<AITool> GetTools(IPluginFactory plugins) =>
      AgentToolFactory.CreateFromObject(plugins.CreateSocialMediaPlugin());
  }
}
