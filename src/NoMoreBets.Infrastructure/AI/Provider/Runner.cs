using System.Globalization;
using MediatR;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Application.Bankroll.GetDaysUntilPayday;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.XApi;

namespace NoMoreBets.Infrastructure.AI.Provider;

public sealed class Runner : IAgentPhaseRunner
{
  private readonly AgentBuilder _agentBuilder;
  private readonly ILogger<Runner> _logger;
  private readonly IMediator _mediator;
  private readonly IPluginFactory _pluginFactory;
  private readonly IAgentSessionContext _agentSessionContext;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IOptions<XApiOptions> _xApiOptions;

  public Runner(
    AgentBuilder agentBuilder,
    IPluginFactory pluginFactory,
    IUnitOfWork unitOfWork,
    IAgentSessionContext agentSessionContext,
    IMediator mediator,
    IOptions<XApiOptions> xApiOptions,
    ILogger<Runner> logger)
  {
    _agentBuilder = agentBuilder;
    _logger = logger;
    _mediator = mediator;
    _pluginFactory = pluginFactory;
    _agentSessionContext = agentSessionContext;
    _unitOfWork = unitOfWork;
    _xApiOptions = xApiOptions;
  }

  public async Task<List<ChatMessageContent>> Chat(string userMessage, CancellationToken cancellationToken = default)
  {
    var config = _agentBuilder.Build();
    config.Agent.Kernel.Plugins.AddFromObject(_pluginFactory.CreateMemoriesPlugin());
    config.Agent.Kernel.Plugins.AddFromObject(_pluginFactory.CreateInternetSearchPlugin());

    var messages = new List<ChatMessageContent>();
    await foreach (var message in config.Agent.InvokeAsync(userMessage, config.Thread, config.Options, cancellationToken))
    {
      messages.Add(message.Message);
    }

    return messages;
  }
  public async Task<IReadOnlyList<IMessage>> RunResearchPhaseAsync(Match match, CancellationToken cancellationToken = default)
  {
    Action<Kernel> configureKernel = kernel =>
    {
      kernel.Plugins.AddFromObject(_pluginFactory.CreateAgentResearchPlugin());
      kernel.Data.Add("matchId", match.Id);
      kernel.Data.Add("phase", "Research");
    };

    var prompt = $"""
          Today is {DateOnly.FromDateTime(DateTime.UtcNow)}.
          You are a long-running betting agent with persistent memory.
          
          You are now conducting match research for yourself, to support your own later betting decision on this match:
          - Match ID: {match.Id}
          - Fixture: {match.HomeClub.Name} (ID: {match.HomeClub.Id}) vs {match.AwayClub.Name} (ID: {match.AwayClub.Id})
          - Kickoff (UTC): {match.MatchDate:yyyy-MM-dd HH:mm}
          
          Important context:
          You are NOT reacting directly to live betting market movements or line shifts during this research phase.
          Because of this, you should assume you do NOT have a timing-based market edge (no late line movement advantage, no sharp market reaction signals).
          Your edge must come only from structural, statistical, tactical, or contextual analysis—not from market positioning or timing.
          
          Goal:
          Create complete, decision-oriented research for this specific match that you will later use in your own betting phase.
          This is your personal prep work: your future self in the betting phase should be able to read this and decide whether to bet or pass.
          
          You must use the available AgentResearchPlugin functions explicitly.

          ## Required workflow (execute in order)

          1) Read memory context first:
          - Call `GetMemoryRecordsAsync`
          - Call `ReadMemoryAsync` for relevant records before new analysis

          2) Build core match intelligence:
          - `GetMatchPreviewAsync`
          - `GetLineupsAsync`
          - `GetInjuriesAsync`
          - `GetHead2HeadStatsAsync`
          - `GetMatchBettingOddsHistoryAsync`
          - `GetLeagueTableAsync`

          3) Build team-level context for both clubs (home and away):
          - `GetClubStatistics`
          - `GetClubRollingPerformanceAsync`
          - `GetClubRecentGamesAsync`
          - `GetClubDailySummaryAsync`

          4) Build news and sentiment context:
          - If needed, call `SearchNewsAsync` to gather the latest news for both the home club and the away club.
          - If additional validation or deeper tactical/context insight is required, you may call `GetWebGroundingAsync` to verify key claims.
          - Focus on separating meaningful signals from noise, assessing source reliability, and spotting potential market overreactions or underreactions.

          5) Synthesize decision-oriented research output stating clear betting implications, including potential value angles and confidence drivers

          6) Save learnings for your future self:
          - Persist reusable insights, patterns, and hypotheses using `AppendMemoryAsync`, `ReplaceMemoryAsync`, or `WriteMemoryAsync`
          - Keep memories concise, structured, and directly useful for your own future research and betting decisions
          - Do not store raw noisy dumps; store distilled insights
          - If needed create new memories with `WriteMemoryAsync`

          7) Completion gate (mandatory):
          - Create one complete final report text for this match - it must be brief and scannable - try to keep it under 500 words.
          - Call `SaveMatchAnalysisAsync` with this match id and the final report content
          - Do not terminate until `SaveMatchAnalysisAsync` succeeds

          8) Finish with short summary of key insights and betting implications.

          ## Quality constraints
          - Be analytical and evidence-driven
          - Cross-check important claims
          - If data is missing, state it explicitly and continue with best-effort reasoning
          - Do not skip required steps

          ### Guardrails
          - Focus on delivering the research output as if for a human analyst, not on describing your own process.
          """;

    var paperBetPrompt = $"""
          Your next step is to create a **paper (fictional) prediction slip** for this match.

          Purpose:
          - This is a **research artifact**, not a real bet.
          - It does NOT affect bankroll in any way.
          - Odds are intentionally unavailable — ignore pricing completely.
          - Your only goal is to **maximize correctness of predictions**.

          Core Instructions:
          - Base all selections strictly on your prior research.
          - Single selections are ok but multiple selections (parlays) are preferred.

          STRICT Tool Flow (must follow exactly):
          1) Call `GetMatchEvents` - to get the available markets and outcome option names
          2) Call `PlaceBetSlip` - to place the slip
          """;

    var result = await ExecuteAgentPhase(
      AgentSessionPhase.Research,
      prompt,
      configureKernel,
      cancellationToken,
      followUp: new AgentPhaseFollowUp(
        kernel =>
        {
          kernel.Plugins.Clear();
          kernel.Plugins.AddFromObject(_pluginFactory.CreateResearchBetPlugin(match.Id));
        },
        paperBetPrompt)).ConfigureAwait(false);
    return result.Messages;
  }

  public async Task<IReadOnlyList<IMessage>> RunUpcomingMatchesInternetResearchAsync(CancellationToken cancellationToken = default)
  {
    Action<Kernel> configureKernel = kernel =>
    {
      kernel.Plugins.AddFromObject(_pluginFactory.CreateAgentInternetResearchPlugin());
      kernel.Data.Add("phase", "InternetResearch");
    };

    var prompt = $"""
          Today is {DateOnly.FromDateTime(DateTime.UtcNow)}.
          You are a long-running betting agent with persistent memory.
          
          You are conducting research for upcoming matches for yourself.
          You are not writing for a syndicate or external audience: this is your own prep for your own future betting sessions.
          Structure it so your future self can quickly reuse it in the betting phase.
          Focus on narratives, news, sentiment, context of the game etc.
          Remember to save the research to memory so you can reuse it in later betting and reflection phases.

          You must use the available plugin functions explicitly.

          Goal:
          Produce one (or more) general research brief(s) for upcoming fixtures that your future self can use for later match-level analysis and betting decisions.

          ## Required workflow

          1) Enumerate upcoming fixtures:
          - Call `GetAvailableMatchesAsync` and identify key upcoming matches to monitor

          2) Read memory context:
          - Call `GetMemoryRecordsAsync`
          - Call `ReadMemoryAsync` for relevant records

          3) Gather internet context:
          - Call `SearchNewsAsync` and `GetWebGroundingAsync` as needed to gather match/club information, news, league updates, and related context
          - Prioritize recent, reliable sources and label uncertainty

          4) Persist useful knowledge:

          - Save distilled, reusable insights to memory with `AppendMemoryAsync`, `ReplaceMemoryAsync`, or `WriteMemoryAsync`
          - Avoid raw copy-paste dumps

          ## Guardrails
          - Be evidence-driven and explicit about missing data
          """;

    var result = await ExecuteAgentPhase(
      AgentSessionPhase.InternetResearch,
      prompt,
      configureKernel,
      cancellationToken).ConfigureAwait(false);

    return result.Messages;
  }

  public async Task<IReadOnlyList<IMessage>> RunMemoryCleanupPhaseAsync(CancellationToken cancellationToken = default)
  {
    int daysCutoff = 2;
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var utcCutoff = DateTime.UtcNow.AddDays(-daysCutoff);

    Action<Kernel> configureKernel = kernel =>
    {
      kernel.Plugins.AddFromObject(_pluginFactory.CreateAgentMemoryMaintenancePlugin());
      kernel.Data.Add("phase", "MemoryCleanup");
    };

    var prompt = $"""
          Today is {today} (UTC calendar date).
          You are a long-running betting agent with persistent memory.

          You are running a maintenance pass: review saved memories and remove or trim content that will no longer be useful.

          Retention rule for match-specific material:
          - Fixture or match-specific content whose **match date / kickoff (interpret as UTC unless clearly local)** was **more than {daysCutoff} days before today** is outside retention for ephemeral notes (e.g. lineups, injury snapshots, narrow “this fixture” narratives, stale pre-match hype, post-mortems that only mattered for that one game).
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

    var result = await ExecuteAgentPhase(
      AgentSessionPhase.MemoryCleanup,
      prompt,
      configureKernel,
      cancellationToken).ConfigureAwait(false);

    return result.Messages;
  }

  public async Task<IReadOnlyList<IMessage>> RunReflectionPhaseAsync(CancellationToken cancellationToken = default)
  {
    var slips = await _unitOfWork.Betting
      .GetNonPendingBetSlipsAwaitingReflectionAsync(cancellationToken)
      .ConfigureAwait(false);
    if (slips.Count == 0)
    {
      _logger.LogInformation(
        "Skipping reflection agent phase: no settled bet slips awaiting reflection (non-pending with no reflection session).");
      return Array.Empty<IMessage>();
    }

    var reflectionBetSlipIds = slips.Select(s => s.Id).ToList();

    var prompt = $"""
          Today is {DateOnly.FromDateTime(DateTime.UtcNow)}.
          You are a long-running betting agent with persistent memory.

          You are running your reflection phase: learn from recent settled outcomes and store only durable, reusable decision rules that improve future performance.
          You must use the available AgentReflectionPlugin functions explicitly.

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

    Action<Kernel> configureKernel = kernel =>
    {
      kernel.Plugins.AddFromObject(_pluginFactory.CreateAgentReflectionPlugin());
      kernel.Data.Add("phase", "Reflection");
    };

    var result = await ExecuteAgentPhase(
      AgentSessionPhase.Reflection,
      prompt,
      configureKernel,
      cancellationToken).ConfigureAwait(false);

    if (reflectionBetSlipIds.Count > 0)
    {
      await _unitOfWork.Betting
        .MarkBetSlipsAgentSessionReflectedAsync(result.SessionId, reflectionBetSlipIds, cancellationToken)
        .ConfigureAwait(false);
    }

    return result.Messages;
  }

  public async Task<IReadOnlyList<IMessage>> RunBettingExecutionPhaseAsync(CancellationToken cancellationToken = default)
  {

    var currentBalance = await _unitOfWork.Bankroll
      .GetCurrentBalanceAsync(cancellationToken)
      .ConfigureAwait(false);
    var daysUntilPayday = await _mediator
      .Send(new GetDaysUntilPaydayQuery(), cancellationToken)
      .ConfigureAwait(false);

    var balanceText = currentBalance.ToString("F2", CultureInfo.InvariantCulture);

    var prompt = $"""
          Today is {DateOnly.FromDateTime(DateTime.UtcNow)}.
          You are a long-running betting agent with persistent memory.
          Current account balance: {balanceText}
          Days until payday: {daysUntilPayday}

          You are executing the betting phase for the portfolio: review every match that is open for betting, align with stored strategy and bankroll rules.
          You may place zero bet slips (pass entirely), exactly one bet slip, or more than one bet slip in this run, as strategy and bankroll allow.
          Each call to `PlaceBetSlip` is one separate bet (one slip) with its own stake. That slip is either a single (one selection, one event market) or a parlay (multiple selections combined on the same slip, across one or more matches). The `betSelections` JSON array must contain at least one selection per slip: one element means a single bet; multiple elements mean a parlay on that slip.
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
    Action<Kernel> configurePlugins = kernel =>
    {
      kernel.Plugins.AddFromObject(_pluginFactory.CreateAgentBettingPlugin());
      kernel.Data.Add("phase", "Betting");
    };

    var result = await ExecuteAgentPhase(
      AgentSessionPhase.Betting,
      prompt,
      configurePlugins,
      cancellationToken).ConfigureAwait(false);
    return result.Messages;
  }

  private async Task<AgentPhaseRunResult> ExecuteAgentPhase(
    AgentSessionPhase phase,
    string userPrompt,
    Action<Kernel> configureKernel,
    CancellationToken cancellationToken = default,
    AgentPhaseFollowUp? followUp = null)
  {
    var config = _agentBuilder.BuildForScheduledJob();
    configureKernel(config.Agent.Kernel);

    var xOAuthConfigured = _xApiOptions.Value.IsOAuthConfigured;

    var phaseName = phase.ToString();
    _logger.LogInformation("Betting agent phase {Phase} starting", phaseName);

    var startedAt = DateTime.UtcNow;
    var sessionId = await _unitOfWork.AgentSessions
      .CreateSessionAsync(phase, startedAt, cancellationToken)
      .ConfigureAwait(false);
    _agentSessionContext.SessionId = sessionId;

    var messages = new List<IMessage>();
    try
    {
      var phaseMessages = await InvokeAndCollectPhaseTranscriptMessagesAsync(config, userPrompt, cancellationToken).ConfigureAwait(false);
      messages.AddRange(phaseMessages);

      if (followUp is not null)
      {
        followUp.ConfigureKernel(config.Agent.Kernel);
        await InvokeAndCollectPhaseTranscriptMessagesAsync(config, followUp.Prompt, cancellationToken).ConfigureAwait(false);
      }

      if (xOAuthConfigured && phase == AgentSessionPhase.Betting)
      {
        config.Agent.Kernel.Plugins.Clear();
        config.Agent.Kernel.Plugins.AddFromObject(_pluginFactory.CreateSocialMediaPlugin());
        var followUpPrompt = """
        If you placed any bets, publish a post on X - call CreateXPost with the post content.
        The post should be a concise summary of the bets you have just placed. 
        Keep the tone professional yet engaging. 
        Always include hashtags for the league involved, derived from that league's name (e.g. Premier League as #PremierLeague, Serie A as #SerieA).
        """;
        await InvokeAndCollectPhaseTranscriptMessagesAsync(config, followUpPrompt, cancellationToken).ConfigureAwait(false);
      }
    }
    finally
    {
      try
      {
        var rows = AgentSessionTranscriptMapper.ToEntities(messages);
        await _unitOfWork.AgentSessions
          .AddMessagesAsync(sessionId, rows, cancellationToken)
          .ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to persist agent session {SessionId} transcript", sessionId);
      }

      _agentSessionContext.SessionId = null;
    }

    _logger.LogInformation(
      "Betting agent phase {Phase} completed with {MessageCount} assistant message(s)",
      phaseName,
      messages.Count);

    return new AgentPhaseRunResult(messages, sessionId);
  }

  private static async Task<List<IMessage>> InvokeAndCollectPhaseTranscriptMessagesAsync(
    AgentConfig config,
    string prompt,
    CancellationToken cancellationToken)
  {
    var messages = new List<IMessage>();
    await foreach (var message in config.Agent.InvokeAsync(prompt, config.Thread, config.Options, cancellationToken)
                     .ConfigureAwait(false))
    {
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
      foreach (var item in message.Message.Items)
      {
        if (item is ReasoningContent reasoning)
        {
          messages.Add(new ReasoningMessage(reasoning.Text));
        }

        if (item is FunctionCallContent functionCall)
        {
          var functionName = functionCall.FunctionName;
          var arguments = functionCall.Arguments?.Select(a => new FunctionArgument(a.Key.ToString(), a.Value?.ToString())).ToList();
          messages.Add(new FunctionMessage(functionName, arguments));
        }
      }

#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
      if (!string.IsNullOrEmpty(message.Message.Content))
      {
        messages.Add(new Message(message.Message.Content));
      }
    }

    return messages;
  }
}

internal sealed record AgentPhaseFollowUp(Action<Kernel> ConfigureKernel, string Prompt);

internal sealed record AgentPhaseRunResult(IReadOnlyList<IMessage> Messages, int SessionId);
