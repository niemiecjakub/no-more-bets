using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using NoMoreBets.Infrastructure.AI.Providers.DailySlip;
using NoMoreBets.Infrastructure.AI.Providers.Date;
using NoMoreBets.Infrastructure.AI.Providers.Todo;
using NoMoreBets.Infrastructure.AI.Providers.WebSearch;
using NoMoreBets.Infrastructure.AI.Tools;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.AI.Phases.DailySlip;

public static class DailySlipPhaseDefinition
{
  public static AgentSessionPhase Phase => AgentSessionPhase.DailySlip;
}

internal sealed class DailySlipExecuteStep : IAgentPhaseStep
{
  public string? AgentInstructions => """
    You produce a daily house betting card.

    Create up to three betting slips:
    - Low Risk
    - Medium Risk
    - High Risk

    Every betting slip should maximize expected value for its intended risk profile. Favor selections where the estimated chance of winning is better than the implied probability from the available odds. Do not choose selections solely because they are likely to win or solely because they offer high odds.

    Risk profiles:

    Low Risk:
    - Optimize for probability of winning.
    - Use the safest supported markets.
    - Prefer fewer selections and lower combined odds.

    Medium Risk
    - Your strongest overall betting opinion.
    - Balance probability and payout.
    - If you could place only one slip, it would be this one.

    High Risk
    - Optimize for payout.
    - Higher combined odds are expected.
    - May include additional legs or more aggressive markets.
    - Accept substantially lower probability of success.
    - Never add speculative legs solely to increase odds.

    Each slip should represent a different betting strategy, not simply the same slip with more selections added.
    The same selection may appear on multiple slips if it remains a strong value play, but the slips must not be identical.
    It is acceptable to omit a risk level if there is no defensible betting opportunity.
    
    For every slip:
    - Estimate the probability that the entire slip wins (0 < p < 1).
    - Be realistic. Do not overestimate confidence.
    - Account for correlation between related legs.
    - Explain why the slip is worth placing.
    - Explain the primary reasons it could lose.

    Use web search only when stored research is insufficient or when late-breaking information (injuries, lineups, suspensions, weather) could materially affect the pick.

    If recent information contradicts stored research, prefer the newer evidence.
  """;

  public string BuildPrompt()
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    return $"""
      Today's date (UTC): {today:yyyy-MM-dd}.
      Use the todo list to track the work.
      1. List today's available matches. Only bet those matches.
      2. Read stored research and current odds for matches you might use. Start with the main markets. Check form or tables only when they would change a pick. Use web search when stored research is thin or a claim you would bet on may be out of date.
      3. Skip matches you cannot price. Choose selections, then build slips.
      4. Place at most one Low, one Medium, and one High paper slip. Skip a level you cannot defend. If you have nothing honest to place, place nothing.
      5. In your closing note, name matches you considered and did not use, one line each.
      """;
  }

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([
      ToolRegistry.Match.GetClubRollingPerformance,
      ToolRegistry.Match.GetLeagueTable,
      ToolRegistry.Match.GetGroupTable,
    ]);

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new DateProvider(),
    new DailySlipProvider(
      serviceProvider.GetRequiredService<DailySlipTool>(),
      serviceProvider.GetRequiredService<BettingTool>()),
    new WebSearchProvider(
      serviceProvider.GetRequiredService<ISearchService>(),
      serviceProvider.GetRequiredService<AgentRunToolMetadataCollector>()),
    new TodoProvider(),
  ];
}
