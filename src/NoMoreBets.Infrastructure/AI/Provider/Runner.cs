using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;

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
  public Task<IReadOnlyList<string>> RunResearchPhaseAsync(CancellationToken cancellationToken = default)
  {
    const string phaseName = "Research";
    //var prompt = $"""
    //             Today is {DateOnly.FromDateTime(DateTime.UtcNow)}.
    //             Use MemoryPlugin to read already stored information.
    //             Use MatchPlugin to dive into upcomming matches.
    //             If you need to find something on the web, use SearchPlugin, you may look into anything you like.

    //             Think about you findings, analyze them and store your insights in the MemoriesPlugin, so you can use them later in the betting phase or in the future.
    //             You may create new memories or append to existing ones, just make sure to keep them updated and well organized.
    //             """;



    var prompt = $"""
        Today is {DateOnly.FromDateTime(DateTime.UtcNow)}.
        
        You are now conducting research for the betting phase.
        
        - Explore upcoming matches.
        - Build knowledge, strategies, and insights for future betting
        - Reflect on your own thinking and refine your approach
        - Do not focus on immediate betting decisions here
        
        ## Exploration
        
        You may:
        - Start by calling MatchPlugin `GetUpcomingMatches` to build the research queue
        - Use MatchPlugin to investigate matches, lineups, injuries, statistics, and trends
        - Use SearchPlugin to gather external news or context
        - Use MemoriesPlugin to read past knowledge, insights, and strategies and store new ones.
        - Revisit hypotheses, challenge assumptions, and explore patterns

        Memory Rules:
        - Memories must improve future research and decisions
        - Avoid storing raw stats, one-off trivialities, or irrelevant emotions
        - Use `Append` to add new insights; `Replace` to evolve or correct prior knowledge
        - Keep structure, clarity, and utility as priorities
        
        ---
        
        ## Thinking & Self-Reflection
        
        Before every action, ask:
        - What am I trying to confirm or disprove?
        - Will this insight improve my knowledge or strategy?
        - Am I mistaking noise for a pattern?
        
        After research, reflect:
        - Did I learn something meaningful?
        - Did any assumptions prove wrong?
        - How can this improve my approach next time?
        
        ---
        
        ## Depth vs Efficiency
        
        - Deep exploration is allowed when something looks interesting
        - Avoid endless digging with diminishing returns
        - Prioritize learning and evolution over immediate outcomes
        
        ---
        
        ## Behavior
        
        - Curious, analytical, and skeptical
        - Focused on patterns, strategy, and market understanding
        - Comfortable leaving questions unanswered if insight value is low
        - Treat information as a cost and memory as an asset
        """;
    Action<Kernel> configurePlugins = kernel =>
    {
      kernel.Plugins.AddFromObject(_pluginFactory.CreateMatchPlugin());
      kernel.Plugins.AddFromObject(_pluginFactory.CreateSearchPlugin());
      kernel.Plugins.AddFromObject(_pluginFactory.CreateMemoriesPlugin());
    };

    return ExecuteBettingPhaseAsync(
      phaseName,
      prompt,
      configurePlugins,
      cancellationToken);
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
