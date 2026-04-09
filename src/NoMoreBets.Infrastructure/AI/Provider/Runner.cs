using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Infrastructure.AI.Provider;

public sealed class Runner : IAgentPhaseRunner
{
  private readonly AgentBuilder _agentBuilder;
  private readonly ILogger<Runner> _logger;
  private readonly IPluginFactory _pluginFactory;

  public Runner(
    AgentBuilder agentBuilder,
    IPluginFactory pluginFactory,
    ILogger<Runner> logger)
  {
    _agentBuilder = agentBuilder;
    _logger = logger;
    _pluginFactory = pluginFactory;
  }

  public async Task<List<ChatMessageContent>> Chat(string userMessage, CancellationToken cancellationToken = default)
  {
    var config = _agentBuilder.Build();
    config.Agent.Kernel.Plugins.AddFromObject(_pluginFactory.CreateMemoriesPlugin());
    config.Agent.Kernel.Plugins.AddFromObject(_pluginFactory.CreateSearchPlugin());

    var messages = new List<ChatMessageContent>();
    await foreach (var message in config.Agent.InvokeAsync(userMessage, config.Thread, config.Options, cancellationToken))
    {
      messages.Add(message.Message);
    }

    return messages;
  }
  public async Task<IReadOnlyList<string>> RunResearchPhaseAsync(Match match, CancellationToken cancellationToken = default)
  {
    const string phaseName = "Research";
    Action<Kernel> configurePlugins = kernel =>
    {
      kernel.Plugins.AddFromObject(_pluginFactory.CreateAgentResearchPlugin());
    };

    var prompt = $"""
          Today is {DateOnly.FromDateTime(DateTime.UtcNow)}.
          
          You are now conducting research for the betting phase for this match:
          - Match ID: {match.Id}
          - Fixture: {match.HomeClub.Name} (ID: {match.HomeClub.Id}) vs {match.AwayClub.Name} (ID: {match.AwayClub.Id})
          - Kickoff (UTC): {match.MatchDate:yyyy-MM-dd HH:mm}
          
          Goal:
          Create complete betting research for this specific match that can directly support a later betting decision.

          You must use the available AgentResearchPlugin functions explicitly.

          ## Required workflow (execute in order)

          1) Read memory context first:
          - Call `GetMemoryRecords`
          - Call `Read` for relevant records before new analysis

          2) Build core match intelligence:
          - `GetMatchPreview`
          - `GetLineups`
          - `GetInjuries`
          - `GetHead2HeadStats`
          - `GetMatchBettingOddsHistory`
          - `GetLeagueTable`

          3) Build team-level context for both clubs (home and away):
          - `GetClubLeagueStatistics`
          - `GetClubRollingPerformance`
          - `GetClubRecentGames`
          - `GetClubDailySummary`

          4) Build news and sentiment context:
          - Call `SearchNews` for:
            - home club latest news
            - away club latest news
            - fixture-specific news (clubs + league + injuries/suspensions keywords)
          - Call `GetWebGrounding` to verify key claims and gather deeper tactical/context insights.
          - Distinguish signal vs noise, confirm reliability, and identify likely market overreaction/underreaction.

          5) Synthesize decision-oriented research output:
          Your final research must include:
          - match state and tactical picture
          - lineup/injury impact and uncertainties
          - form and team-strength profile
          - head-to-head context (with caution about small sample bias)
          - market/odds movement interpretation
          - current news sentiment and key narratives
          - risks, unknowns, and what could invalidate the view
          - clear betting implications (not bet placement), including potential value angles and confidence drivers

          6) Save learnings to memory:
          - Persist reusable insights, patterns, and hypotheses using `Append`, `Replace`, or `Write`
          - Keep memories concise, structured, and useful for future research and betting decisions
          - Do not store raw noisy dumps; store distilled insights

          7) Completion gate (mandatory):
          - Create one complete final report text for this match
          - Call `SaveMatchAnalysis` with this match id and the final report content
          - Do not terminate until `SaveMatchAnalysis` succeeds

          ## Quality constraints
          - Be analytical, skeptical, and evidence-driven
          - Cross-check important claims across multiple tool outputs
          - If data is missing, state it explicitly and continue with best-effort reasoning
          - Do not skip required steps
          """;

    return await ExecuteBettingPhaseAsync(
      $"{phaseName}:{match.Id}",
      prompt,
      configurePlugins,
      cancellationToken).ConfigureAwait(false);
  }

  public Task<IReadOnlyList<string>> RunReflectionPhaseAsync(CancellationToken cancellationToken = default)
  {
    const string phaseName = "Reflection";
    var prompt = """
                 You are running the reflection phase.
                 
                 Goal:
                 Improve future decisions.
                 
                 Steps:
                 
                 1. Call GetMemoryRecords
                 2. Read STRATEGY and REFLECTIONS
                 3. Call GetBetSlips with status Won, then with status Lost (or once with no filter if you prefer, then group mentally)
                 4. For each settled bet:
                 
                    * Compare expected vs actual
                    * Evaluate decision quality
                 
                 5. Identify:
                 
                    * Mistakes
                    * Biases
                    * Patterns
                 
                 6. Summarize findings
                 7. Append or update REFLECTIONS with durable lessons
                 8. Promote repeated patterns to KNOWLEDGE when justified
                 
                 Constraints:
                 
                 * Do not overreact to single results
                 * Focus on long-term performance and process quality
                 """;
    Action<Kernel> configurePlugins = kernel =>
    {
      kernel.Plugins.AddFromObject(_pluginFactory.CreateBettingPlugin());
      kernel.Plugins.AddFromObject(_pluginFactory.CreateMemoriesPlugin());
      kernel.Plugins.AddFromObject(_pluginFactory.CreateSearchPlugin());
    };

    return ExecuteBettingPhaseAsync(
      phaseName,
      prompt,
      configurePlugins,
      cancellationToken);
  }

  public Task<IReadOnlyList<string>> RunBettingExecutionPhaseAsync(CancellationToken cancellationToken = default)
  {
    const string phaseName = "BettingExecution";
    var prompt = """
                 You are running the betting phase.
                 
                 Goal:
                 Select and execute high-quality bets.
                 
                 Steps:
                 
                 1. Call GetMemoryRecords
                 2. Read STRATEGY, BANKROLL_MANAGEMENT, KNOWLEDGE, REFLECTIONS
                 3. Call GetCurrentBalance
                 4. Call GetAvailableMatches
                 5. For each match:
                 
                    * GetMatchAnalysis
                    * GetCurrentOdds
                    * Use MatchPlugin tools when they improve confidence (lineups, injuries, etc.)
                 
                 6. Evaluate:
                 
                    * Value edge
                    * Strategy alignment
                    * Confidence
                 
                 7. Decision:
                 
                    * If NO -> skip
                    * If YES:
                 
                      * Determine stake
                      * Call PlaceBetSlip
                 
                 8. Store insights in KNOWLEDGE or MEMORIES as appropriate
                 
                 Constraints:
                 
                 * No weak bets
                 * Respect bankroll (stake must not exceed GetCurrentBalance)
                 * Avoid duplicate or redundant positions on the same outcome when it is not justified
                 """;
    Action<Kernel> configurePlugins = kernel =>
    {
      kernel.Plugins.AddFromObject(_pluginFactory.CreateMatchPlugin());
      kernel.Plugins.AddFromObject(_pluginFactory.CreateBettingPlugin());
      kernel.Plugins.AddFromObject(_pluginFactory.CreateBankrollPlugin());
      kernel.Plugins.AddFromObject(_pluginFactory.CreateMemoriesPlugin());
      kernel.Plugins.AddFromObject(_pluginFactory.CreateSearchPlugin());
    };

    return ExecuteBettingPhaseAsync(
      phaseName,
      prompt,
      configurePlugins,
      cancellationToken);
  }

  private async Task<IReadOnlyList<string>> ExecuteBettingPhaseAsync(
    string phaseName,
    string userPrompt,
    Action<Kernel> configurePlugins,
    CancellationToken cancellationToken = default)
  {
    var config = _agentBuilder.BuildForScheduledJob();
    configurePlugins(config.Agent.Kernel);

    var messages = new List<string>();
    _logger.LogInformation("Betting agent phase {Phase} starting", phaseName);

    await foreach (var message in config.Agent.InvokeAsync(userPrompt, config.Thread, config.Options, cancellationToken)
                     .ConfigureAwait(false))
    {
      messages.Add(message.Message.Content ?? string.Empty);
    }

    _logger.LogInformation(
      "Betting agent phase {Phase} completed with {MessageCount} assistant message(s)",
      phaseName,
      messages.Count);

    return messages;
  }
}
