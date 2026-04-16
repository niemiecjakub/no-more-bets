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
          
          Goal:
          Create complete, decision-oriented research for this specific match that you will later use in your own betting phase.
          This is your personal prep work: your future self in betting phase should be able to read this and decide whether to bet or pass.
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
          - Be analytical, skeptical, and evidence-driven
          - Cross-check important claims
          - If data is missing, state it explicitly and continue with best-effort reasoning
          - Do not skip required steps

          ### Guardrails
          - In response and reasoning do not mention the internal process, tool names etc. Focus on delivering the research output as if for a human analyst, not on describing your own process.
          """;

    var result = await ExecuteAgentPhase(
      AgentSessionPhase.Research,
      prompt,
      configureKernel,
      cancellationToken).ConfigureAwait(false);
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
          
          You are conducting research for upcoming Premier League matches for yourself.
          You are not writing for a syndicate or external audience: this is your own prep for your own future betting sessions.
          Structure it so your future self can quickly reuse it in the betting phase.
          Focus on narratives, news, sentiment, context of the game etc.
          Remember to save the research to memory so you can reuse it in later betting and reflection phases.

          You must use the available plugin functions explicitly.

          Goal:
          Produce one general research brief for upcoming fixtures that your future self can use for later match-level analysis and betting decisions.

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
          - Do not mention internal tool names or process in final narrative
          """;

    var result = await ExecuteAgentPhase(
      AgentSessionPhase.InternetResearch,
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

          You are running your own reflection phase for the portfolio: learn from your recent settled outcomes, then persist durable process lessons to memory.
          This reflection is for your own improvement loop: better future research, better future betting decisions.
          You must use the available AgentReflectionPlugin functions explicitly.

          Goal:
          Improve your own future decision quality (calibration, discipline, edge definition) without chasing short-term noise. Treat single outcomes as weak evidence unless the failure mode is clearly process-related.
          Think explicitly about improvements for upcoming work: what should change in **future research** (how matches are framed, which evidence is gathered, how confident the write-up should be) and in **future betting** (when to bet or pass, sizing, overlap with pending slips, use of odds and bankroll rules). Turn the durable parts of that thinking into memory so the next phases can act on it.

          ## Required workflow (execute in order)

          1) Get bet slips awaiting reflection:
          - Call `GetBetSlipsAwaitingReflectionAsync`.

          2) Read memory context:
          - Call `GetMemoryRecordsAsync`
          - Call `ReadMemoryAsync` for at least: STRATEGY, REFLECTIONS, GENERAL_KNOWLEDGE and other memories you need.

          3) For each settled slip (and each selection as needed):
          - Compare implied edge at placement (odds, stake, structure) versus outcome and strategy rules
          - For distinct match IDs involved, call `GetMatchResearchTextAsync` when available to contrast the pre-match thesis with what happened
          - Whenever it helps judgment, you may call `SearchNewsAsync` and/or `GetWebGroundingAsync` with focused queries—not only to verify a disputed fact, but also to clarify context, resolve ambiguities, or dive deeper on tactics, squad news, or match narratives that bear on why the slip won or lost

          4) Synthesize:
          - Process mistakes vs bad luck (variance); recurring biases
          - What would you change in decision rules going forward (specific, testable)
          - Concrete improvements for the next **research** cycle and the next **betting** cycle (even if some items are tentative, label uncertainty)

          5) Persist lessons:
          - Update any memories or add new as appropriate
          - Keep entries concise and actionable; avoid dumping raw tool output into memory

          6) Finish with a short summary for a human: main lessons, what you would watch in the next betting cycle.

          ## Quality constraints
          - Weight sample size: do not overfit one-off results
          - Cross-check conclusions against STRATEGY and BANKROLL_MANAGEMENT
          - If data is missing (no slips, no research text), state it and still improve written reflections where justified

          ### Guardrails
          - In your final narrative to the user, do not mention internal process, tool names, or plugin mechanics.
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

          You are executing the betting phase for the portfolio: review every match that is open for betting, align with stored strategy and bankroll rules, and place bets only when the edge justifies it.
          You may place zero bet slips (pass entirely), exactly one bet slip, or more than one bet slip in this run, as strategy and bankroll allow.
          Each call to `PlaceBetSlip` is one separate bet (one slip) with its own stake. That slip is either a single (one selection, one event market) or a parlay (multiple selections combined on the same slip, across one or more matches). The `betSelections` JSON array must contain at least one selection per slip: one element means a single bet; multiple elements mean a parlay on that slip.
          You must use the available plugin functions explicitly.

          Goal:
          Select and execute only high-conviction, strategy-aligned bets. When edge, confidence, or alignment is weak, pass or place fewer slips rather than forcing marginal bets.

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

          4) For each available match, build a decision picture:
          - call `GetMatchAnalysisAsync` for the match ID, and then call `GetCurrentOddsAsync` if you want to check current odds for this match.
          - If late-breaking information could materially change the thesis versus the stored analysis, use `SearchNewsAsync` and/or `GetWebGroundingAsync` with focused queries.
 
          5) Evaluate each candidate selection:
          - Value vs current prices (implied probability vs your view)
          - Alignment with STRATEGY, BANKROLL_MANAGEMENT, REFLECTIONS and GENERAL_KNOWLEDGE
          - Confidence and what would invalidate the view
          - Stake feasibility: stake must be > 0 and must not exceed `GetCurrentBalance`; respect BANKROLL_MANAGEMENT (unit sizing, max stake, concentration)
          - Overlap with pending slips from `GetBetSlipsAsync`: do not add redundant positions on the same outcome unless clearly justified

          6) Decision:
          - If nothing qualifies: place no slips; summarize the pass in analyst terms (no tool dump)
          - If one or more opportunities qualify: place one slip per distinct bet you want (zero to many slips in total). For each slip, choose stake and build `betSelections`: one item for a single, several items for a parlay on that slip
          - Call `PlaceBetSlip` once per slip with valid JSON as described on the function. Never call `PlaceBetSlip` with an empty `betSelections` array
          - If you place multiple slips, call `GetCurrentBalance` before each further `PlaceBetSlip` so stakes stay within the updated balance after prior stakes

          7) Persist learnings:
          - Update durable insights with `AppendMemoryAsync`, `ReplaceMemoryAsync`, or `WriteMemoryAsync` as appropriate.
          - You may create new memories with `WriteMemoryAsync` if needed.
          - Store distilled takeaways, not raw tool output

          8) Finish with a short summary for a human: how many slips you placed (if any), singles vs parlays, key rationale, and main risks.

          ## Quality constraints
          - Do not skip memory, balance, or match/odds steps for matches you seriously consider

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
    CancellationToken cancellationToken = default)
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
      var phaseMessages = await CollectInvocationMessagesAsync(config, userPrompt, cancellationToken).ConfigureAwait(false);
      messages.AddRange(phaseMessages);

      if (xOAuthConfigured)
      {
        config.Agent.Kernel.Plugins.Clear();
        config.Agent.Kernel.Plugins.AddFromObject(_pluginFactory.CreateMemoriesPlugin());
        config.Agent.Kernel.Plugins.AddFromObject(_pluginFactory.CreateInternetSearchPlugin());
        config.Agent.Kernel.Plugins.AddFromObject(_pluginFactory.CreateSocialMediaPlugin());
        var followUpPrompt = $"""
        If you'd like to publish a post on X, call `CreateXPost` with the post content.

        The post should not present raw data. Instead, share your insights, opinions, or observations—write it as if it's your personal blog.

        If you mention the match/clubs, include hashtags using the league name and club names (prefixed with #).
        """;
        var followUpMessages = await CollectInvocationMessagesAsync(
          config,
          followUpPrompt,
          cancellationToken).ConfigureAwait(false);
        messages.AddRange(followUpMessages);
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

  private static async Task<List<IMessage>> CollectInvocationMessagesAsync(
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

internal sealed record AgentPhaseRunResult(IReadOnlyList<IMessage> Messages, int SessionId);
