using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NoMoreBets.Application.AgentTools;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.AI.Providers.DailySlip;

public sealed class DailySlipProvider : AIContextProvider
{
  private static string BuildInstructions(bool includePlacement) =>
      $$"""
        # Daily slip
        You have access to daily slip tools for today's house card.

        Use these tools to build today's card:
        - Use {{AgentToolCatalog.Betting.GetAvailableMatches.Name}} to list matches kicking off today that have research and current odds. Only bet these matches.
        - Use {{AgentToolCatalog.Betting.GetCurrentOdds.Name}} to check current odds for a match.
        - Use {{AgentToolCatalog.Betting.GetCurrentOddsForMarket.Name}} to check current odds for a single market.
        - Use {{AgentToolCatalog.Betting.GetMatchAnalysis.Name}} to read saved match analysis.
        {{(includePlacement
            ? $"- Use {AgentToolCatalog.DailySlip.PlaceBetSlip.Name} to place one paper slip. Call once per Low, Medium, or High. There is no bankroll."
            : string.Empty)}}

        """;

  private readonly DailySlipTool _dailySlipTool;
  private readonly BettingTool _bettingTool;
  private readonly bool _includePlacement;

  public DailySlipProvider(
    DailySlipTool dailySlipTool,
    BettingTool bettingTool,
    bool includePlacement = true)
  {
    _dailySlipTool = dailySlipTool;
    _bettingTool = bettingTool;
    _includePlacement = includePlacement;
  }

  protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
  {
    var aiContext = new AIContext
    {
      Instructions = BuildInstructions(_includePlacement),
      Tools = CreateTools(),
    };

    return ValueTask.FromResult(aiContext);
  }

  internal IReadOnlyList<string> GetToolNames() =>
    CreateTools().Select(t => t.Name).ToList();

  private AITool[] CreateTools()
  {
    var serializerOptions = AgentAbstractionsJsonUtilities.DefaultOptions;

    var tools = new List<AITool>
    {
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
        _bettingTool.GetCurrentOddsForMarketAsync,
        new AIFunctionFactoryOptions
        {
          Name = AgentToolCatalog.Betting.GetCurrentOddsForMarket.Name,
          Description = "Returns current odds for a single market on the match. Prefer this over GetCurrentOdds when you already know the market. Empty when that market has no stored odds.",
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
    };

    if (_includePlacement)
    {
      tools.Add(
        AIFunctionFactory.Create(
          _dailySlipTool.PlaceBetSlip,
          new AIFunctionFactoryOptions
          {
            Name = AgentToolCatalog.DailySlip.PlaceBetSlip.Name,
            Description = "Places one paper daily slip for a risk tier. Call once per Low/Medium/High.",
            SerializerOptions = serializerOptions,
          }));
    }

    return tools.ToArray();
  }
}
