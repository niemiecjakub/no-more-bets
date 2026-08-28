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
        You build today's house card. Tools:
        - {{AgentToolCatalog.Betting.GetAvailableMatches.Name}} — matches kicking off today that have research and odds. Use only these.
        - {{AgentToolCatalog.Betting.GetCurrentOdds.Name}} — current prices. Copy eventTypeName and option labels exactly.
        - {{AgentToolCatalog.Betting.GetMatchAnalysis.Name}} — stored research for a match on that list.
        - {{AgentToolCatalog.DailySlip.PlaceBetSlip.Name}} — paper slip, stake 10, required riskLevel Low/Medium/High. One slip per tier. No bankroll.
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
    var serializerOptions = AgentAbstractionsJsonUtilities.DefaultOptions;
    var aiContext = new AIContext
    {
      Instructions = Instructions,
      Tools =
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
            Description = "Returns current odds for the match. Compact markets by default.",
            SerializerOptions = serializerOptions,
          }),
        AIFunctionFactory.Create(
          _bettingTool.GetMatchAnalysisAsync,
          new AIFunctionFactoryOptions
          {
            Name = AgentToolCatalog.Betting.GetMatchAnalysis.Name,
            Description = "Returns stored research for the given match.",
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
      ],
    };

    return ValueTask.FromResult(aiContext);
  }
}
