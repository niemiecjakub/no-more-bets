using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Betting.Common;
using NoMoreBets.Application.Betting.GetBetSlipsList;
using NoMoreBets.Application.Betting.GetBettingPerformanceStats;
using NoMoreBets.Application.Betting.GetMatchBettingOddsHistory;
using NoMoreBets.Application.Betting.ResearchBetScenarioStats;
using NoMoreBets.Application.Matches.GetMatchAgentResearch;
using NoMoreBets.Application.Matches.MatchExists;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api")]
public class BettingController(IMediator mediator) : ControllerBase
{
  [HttpGet("bet-slips")]
  public async Task<ActionResult<IReadOnlyList<BetSlipListItemDto>>> GetBetSlips(
    CancellationToken cancellationToken = default)
  {
    var result = await mediator.Send(new GetBetSlipsListQuery(), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("betting/performance-stats")]
  public async Task<ActionResult<BettingPerformanceStatsDto>> GetPerformanceStats(
    CancellationToken cancellationToken = default)
  {
    var result = await mediator.Send(new GetBettingPerformanceStatsQuery(), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matchinsights/matches/{matchId:int}/agent-research")]
  public async Task<ActionResult<MatchResearchOutputDto?>> GetAgentResearch(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await mediator.Send(new MatchExistsQuery(matchId), cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetMatchAgentResearchQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matchinsights/matches/{matchId:int}/betting-odds-history")]
  public async Task<ActionResult<IReadOnlyList<MarketPriceHistory>?>> GetBettingOddsHistory(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    if (!await mediator.Send(new MatchExistsQuery(matchId), cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetMatchBettingOddsHistoryQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matchinsights/matches/{matchId:int}/research-bet-slip")]
  public async Task<ActionResult<MatchResearchBetSlipDto>> GetResearchBetSlip(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await mediator.Send(new MatchExistsQuery(matchId), cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetMatchResearchBetSlipWithScenariosQuery(matchId), cancellationToken).ConfigureAwait(false);
    return result is null ? NotFound() : Ok(result);
  }
}
