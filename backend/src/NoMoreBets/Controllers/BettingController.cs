using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Betting.Common;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Betting.GetBetSlipsList;
using NoMoreBets.Application.Betting.GetMatchBettingOddsHistory;
using NoMoreBets.Application.Betting.GetMatchResearchBetSlip;
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
  public async Task<ActionResult<BetSlipSummary>> GetResearchBetSlip(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await mediator.Send(new MatchExistsQuery(matchId), cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetMatchResearchBetSlipQuery(matchId), cancellationToken).ConfigureAwait(false);
    return result is null ? NotFound() : Ok(result);
  }
}
