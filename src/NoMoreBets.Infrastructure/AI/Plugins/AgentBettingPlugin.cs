using System.ComponentModel;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.AI.Plugins.Models;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class AgentBettingPlugin : AgentPluginBase
{
  private readonly BettingPlugin _bettingPlugin;
  private readonly IUnitOfWork _unitOfWork;

  public AgentBettingPlugin(
    BettingPlugin bettingPlugin,
    IUnitOfWork unitOfWork,
    MemoriesPlugin memoriesPlugin,
    InternetSearchPlugin searchPlugin)
    : base(memoriesPlugin, searchPlugin)
  {
    _bettingPlugin = bettingPlugin;
    _unitOfWork = unitOfWork;
  }

  [KernelFunction]
  [Description("Retrieves matches for which bets can currently be placed.")]
  public async Task<IReadOnlyList<AvailableMatch>> GetAvailableMatchesAsync(CancellationToken cancellationToken = default)
  {
    return await _bettingPlugin.GetAvailableMatchesAsync(cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Returns the current betting odds for the given match.")]
  public async Task<IReadOnlyList<CurrentOddsMarket>> GetCurrentOddsAsync(int matchId, CancellationToken cancellationToken = default)
  {
    return await _bettingPlugin.GetCurrentOddsAsync(matchId, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Returns the latest research analysis content for the given match as plain text.")]
  public async Task<string?> GetMatchAnalysisAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var analysis = await _unitOfWork.Matches
      .GetLatestMatchAnalysisByCodeAsync(matchId, MatchAnalysis.ResearchCode, cancellationToken)
      .ConfigureAwait(false);
    return analysis?.Content;
  }

  [KernelFunction]
  [Description("Places one bet slip per call. One selection is a single bet; multiple selections combine as a parlay on that slip. Call once per slip; you may call multiple times for multiple separate slips. Stake must not exceed current balance.")]
  public async Task PlaceBetSlip(
    decimal stakeAmount,
    [Description("JSON object with property betSelections: an array of selection objects. Each object must have: matchId (int, from GetAvailableMatchesAsync), eventType (string enum name), eventOption (string BettingEventOption enum name). Example: {\"betSelections\":[{\"matchId\":39,\"eventType\":\"bothTeamsToScore\",\"eventOption\":\"bothTeamsToScore_Yes\"}]}")]
    string betSelectionsJson,
    CancellationToken cancellationToken = default)
  {
    await _bettingPlugin.PlaceBetSlip(stakeAmount, betSelectionsJson, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Returns pending bet slips, newest first.")]
  public async Task<IReadOnlyList<BetSlipSummary>> GetBetSlipsAsync(CancellationToken cancellationToken = default)
  {
    return await _bettingPlugin.GetBetSlipsAsync(BetStatus.Pending, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Returns settled bet slips (Won, Lost) created within the last N days")]
  public async Task<IReadOnlyList<BetSlipSummary>> GetNonPendingBetSlipsFromLastDaysAsync(
    [Description("Number of days to look back from now; must be greater than zero.")]
    int lastDays,
    CancellationToken cancellationToken = default)
  {
    return await _bettingPlugin.GetNonPendingBetSlipsFromLastDaysAsync(lastDays, cancellationToken).ConfigureAwait(false);
  }
}
