using System.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Matches.GetMatchAgentResearch;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Infrastructure.AI.Plugins.Models;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class AgentBettingPlugin : AgentPluginBase
{
  private readonly BettingPlugin _bettingPlugin;
  private readonly IMediator _mediator;
  private readonly ILogger<AgentBettingPlugin> _logger;

  public AgentBettingPlugin(
    BettingPlugin bettingPlugin,
    IMediator mediator,
    MemoriesPlugin memoriesPlugin,
    InternetSearchPlugin searchPlugin,
    ILogger<AgentBettingPlugin> logger)
    : base(memoriesPlugin, searchPlugin)
  {
    _bettingPlugin = bettingPlugin;
    _mediator = mediator;
    _logger = logger;
  }

  [KernelFunction]
  [Description("Retrieves matches for which bets can currently be placed.")]
  public async Task<IReadOnlyList<AvailableMatch>> GetAvailableMatchesAsync(CancellationToken cancellationToken = default)
  {
    return await _bettingPlugin.GetAvailableMatchesAsync(cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Returns current odds for the match. Default is compact (1X2, BTTS, double chance, O/U). Pass includeExoticMarkets true only for handicap or exact-score bets.")]
  public async Task<IReadOnlyList<CurrentOddsMarket>> GetCurrentOddsAsync(
    int matchId,
    [Description("Omit or false for compact odds (saves tokens). True includes Handicap and ExactScore.")]
    bool includeExoticMarkets = false,
    CancellationToken cancellationToken = default)
  {
    return await _bettingPlugin.GetCurrentOddsAsync(matchId, includeExoticMarkets, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Returns the latest research analysis content for the given match as plain text.")]
  public async Task<string?> GetMatchAnalysisAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var analysis = await _mediator
      .Send(new GetMatchAgentResearchQuery(matchId), cancellationToken)
      .ConfigureAwait(false);

    if (analysis is null)
    {
      _logger.LogError("No research analysis found for match {MatchId}.", matchId);
      return "Match analysis is not available.";
    }

    return analysis;
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
