using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NoMoreBets.Application.AgentTools;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.AI.Providers.Betting;

public sealed class BettingProvider : AIContextProvider
{
  private static readonly string Instructions =
      $$"""
        # Betting
        You have access to betting tools for evaluating opportunities and placing bets.

        Use these tools to in betting phase execution:
        - Use {{AgentToolCatalog.Betting.GetAvailableMatches.Name}} to browse matches available for betting.
        - Use {{AgentToolCatalog.Betting.GetCurrentOdds.Name}} to check current odds for a match.
        - Use {{AgentToolCatalog.Betting.GetMatchAnalysis.Name}} to read saved match analysis.
        - Use {{AgentToolCatalog.Betting.PlaceBetSlip.Name}} to place a bet. Call {{AgentToolCatalog.Bankroll.GetBalance.Name}} first to confirm stake fits your balance.
        - Use {{AgentToolCatalog.Betting.GetBetSlips.Name}} to review existing bet slips and exposure.
        
        """;

  private readonly BettingTool _bettingTool;

  public BettingProvider(BettingTool bettingTool)
  {
    _bettingTool = bettingTool;
  }

  protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
  {
    var aiContext = new AIContext
    {
      Instructions = Instructions,
      Tools = CreateTools(),
    };

    return ValueTask.FromResult(aiContext);
  }

  private AITool[] CreateTools()
  {
    var serializerOptions = AgentAbstractionsJsonUtilities.DefaultOptions;

    return
    [
      AIFunctionFactory.Create(
        _bettingTool.GetAvailableMatchesAsync,
        new AIFunctionFactoryOptions
        {
          Name = AgentToolCatalog.Betting.GetAvailableMatches.Name,
          Description = "Retrieves matches for which bets can currently be placed.",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        _bettingTool.GetCurrentOddsAsync,
        new AIFunctionFactoryOptions
        {
          Name = AgentToolCatalog.Betting.GetCurrentOdds.Name,
          Description = "Returns current odds for the match. By default returns compact markets (1X2, BTTS, double chance, O/U goals). Set includeExoticMarkets true only when you need handicap or exact-score lines.",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        _bettingTool.GetMatchAnalysisAsync,
        new AIFunctionFactoryOptions
        {
          Name = AgentToolCatalog.Betting.GetMatchAnalysis.Name,
          Description = "Returns structured match analysis for the given match.",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        _bettingTool.PlaceBetSlip,
        new AIFunctionFactoryOptions
        {
          Name = AgentToolCatalog.Betting.PlaceBetSlip.Name,
          Description = "Places one bet slip per call. One selection is a single bet; multiple selections combine as a parlay on that slip. Call once per slip; you may call multiple times for multiple separate slips.",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        _bettingTool.GetBetSlipsAsync,
        new AIFunctionFactoryOptions
        {
          Name = AgentToolCatalog.Betting.GetBetSlips.Name,
          Description = "Returns bet slips, newest first. Optional status: Pending, Won, Lost — omit the argument to return slips in every status.",
          SerializerOptions = serializerOptions,
        }),
    ];
  }
}
