using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NoMoreBets.Application.AgentTools;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.AI.Providers.DailySlip;

public sealed class DailySlipProvider : AIContextProvider
{
  private static readonly string Instructions =
      $$"""
        # Daily slip
        You have access to daily slip tools for today's house card.

        Use these tools to build today's card:
        - Use {{AgentToolCatalog.Betting.GetAvailableMatches.Name}} to list matches kicking off today that have research and current odds. Only bet these matches.
        - Use {{AgentToolCatalog.Betting.GetCurrentOdds.Name}} to check current odds for a match.
        - Use {{AgentToolCatalog.Betting.GetMatchAnalysis.Name}} to read saved match analysis.
        - Use {{AgentToolCatalog.DailySlip.PlaceBetSlip.Name}} to place one paper slip. Stake is always 10. Call once per Low, Medium, or High. There is no bankroll.

        """;

  private readonly DailySlipTool _dailySlipTool;
  private readonly BettingTool _bettingTool;

  public DailySlipProvider(DailySlipTool dailySlipTool, BettingTool bettingTool)
  {
    _dailySlipTool = dailySlipTool;
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
        _dailySlipTool.GetAvailableMatchesAsync,
        new AIFunctionFactoryOptions
        {
          Name = AgentToolCatalog.Betting.GetAvailableMatches.Name,
          Description = "Retrieves matches kicking off today that have research and current odds.",
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
        _dailySlipTool.PlaceBetSlip,
        new AIFunctionFactoryOptions
        {
          Name = AgentToolCatalog.DailySlip.PlaceBetSlip.Name,
          Description = "Places one paper daily slip for a risk tier. Stake is always 10. Call once per Low/Medium/High.",
          SerializerOptions = serializerOptions,
        }),
    ];
  }
}
