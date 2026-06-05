using System.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Matches.GetMatchAgentResearch;
using NoMoreBets.Application.Common;
namespace NoMoreBets.Infrastructure.AI.Plugins;

public class AgentReflectionPlugin : AgentPluginBase
{
  private readonly BettingPlugin _bettingPlugin;
  private readonly IMediator _mediator;
  private readonly ILogger<AgentReflectionPlugin> _logger;

  public AgentReflectionPlugin(
    BettingPlugin bettingPlugin,
    MemoriesPlugin memoriesPlugin,
    InternetSearchPlugin searchPlugin,
    BankrollPlugin bankrollPlugin,
    IMediator mediator,
    ILogger<AgentReflectionPlugin> logger)
    : base(memoriesPlugin, searchPlugin, bankrollPlugin)
  {
    _bettingPlugin = bettingPlugin;
    _mediator = mediator;
    _logger = logger;
  }

  [AgentTool]
  [Description("Returns settled bet slips that still need reflection. Call this first to determine which slips to analyze.")]
  public async Task<IReadOnlyList<BetSlipSummary>> GetBetSlipsAwaitingReflectionAsync(
    CancellationToken cancellationToken = default)
  {
    return await _bettingPlugin.GetBetSlipsAwaitingReflectionAsync(cancellationToken).ConfigureAwait(false);
  }

  [AgentTool]
  [Description("Returns the latest stored research analysis text for the match (same source used before betting). Use to compare pre-match thesis to how the bet resolved.")]
  public async Task<string?> GetMatchResearchTextAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var analysis = await _mediator
      .Send(new GetMatchAgentResearchQuery(matchId), cancellationToken)
      .ConfigureAwait(false);

    if (analysis is null)
    {
      _logger.LogError("No reflection research text found for match {MatchId}.", matchId);
      return "Match analysis is not available.";
    }

    return analysis;
  }
}
