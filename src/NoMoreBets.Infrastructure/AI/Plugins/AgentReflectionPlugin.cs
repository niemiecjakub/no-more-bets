using System.ComponentModel;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;
namespace NoMoreBets.Infrastructure.AI.Plugins;

public class AgentReflectionPlugin : AgentPluginBase
{
  private readonly BettingPlugin _bettingPlugin;
  private readonly IUnitOfWork _unitOfWork;

  public AgentReflectionPlugin(
    BettingPlugin bettingPlugin,
    MemoriesPlugin memoriesPlugin,
    SearchPlugin searchPlugin,
    IUnitOfWork unitOfWork)
    : base(memoriesPlugin, searchPlugin)
  {
    _bettingPlugin = bettingPlugin;
    _unitOfWork = unitOfWork;
  }

  [KernelFunction]
  [Description("Returns settled bet slips that still need reflection. Call this first to determine which slips to analyze.")]
  public async Task<IReadOnlyList<BetSlipSummary>> GetBetSlipsAwaitingReflectionAsync(
    CancellationToken cancellationToken = default)
  {
    return await _bettingPlugin.GetBetSlipsAwaitingReflectionAsync(cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Returns the latest stored research analysis text for the match (same source used before betting). Use to compare pre-match thesis to how the bet resolved.")]
  public async Task<string?> GetMatchResearchTextAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var analysis = await _unitOfWork.Matches
      .GetLatestMatchAnalysisByCodeAsync(matchId, MatchAnalysis.ResearchCode, cancellationToken)
      .ConfigureAwait(false);
    return analysis?.Content;
  }
}
