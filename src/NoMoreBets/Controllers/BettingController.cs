using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Application.Betting.GetMatchBettingOddsHistory;
using NoMoreBets.Application.Matches.GetMatchAgentResearch;
using NoMoreBets.Application.Matches.RunMatchAgentResearch;
using NoMoreBets.Controllers.Models;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches.Dto;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api")]
public class BettingController(AppDbContext db, IMediator mediator) : ControllerBase
{
  [HttpGet("database/bet-slips")]
  public async Task<ActionResult<IReadOnlyList<BetSlipListItemDto>>> GetBetSlips(CancellationToken cancellationToken = default)
  {
    var slips = await db.BetSlip
      .Include(s => s.BetStatusEntity)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m.AwayClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.BetStatusEntity)
      .OrderByDescending(s => s.CreatedAt)
      .ToListAsync(cancellationToken);

    var result = slips
      .Select(s => new BetSlipListItemDto(
        s.Id,
        s.CreatedAt,
        s.StakeAmount,
        s.TotalOdds,
        s.PotentialPayout,
        s.StatusId,
        s.BetStatusEntity.Name,
        s.Selections
          .OrderBy(sel => sel.Id)
          .Select(sel => new BetSelectionItemDto(
            sel.MatchId,
            sel.Match.HomeClub.Name,
            sel.Match.AwayClub.Name,
            sel.Match.HomeClub.Slug,
            sel.Match.AwayClub.Slug,
            BettingEventTypeDisplay.GetDisplayName(sel.BetEventType),
            BettingEventOptionDisplay.GetDisplayName(sel.BetEventOption, sel.Match.HomeClub.Name, sel.Match.AwayClub.Name),
            sel.OddsAtPlacement,
            sel.StatusId,
            sel.BetStatusEntity.Name))
          .ToList(),
        s.AgentSessionId))
      .ToList();

    return Ok(result);
  }

  [HttpGet("matchinsights/matches/{matchId:int}/agent-research")]
  public async Task<ActionResult<string?>> GetAgentResearch(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetMatchAgentResearchQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpPost("matchinsights/matches/{matchId:int}/agent-research/run")]
  public async Task<ActionResult> RunAgentResearch(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    await mediator.Send(new RunMatchAgentResearchCommand(matchId), cancellationToken).ConfigureAwait(false);
    return Accepted();
  }

  [HttpGet("matchinsights/matches/{matchId:int}/betting-odds-history")]
  public async Task<ActionResult<IReadOnlyList<MarketPriceHistory>?>> GetBettingOddsHistory(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetMatchBettingOddsHistoryQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  private Task<bool> MatchExists(int matchId, CancellationToken cancellationToken) =>
    db.Match.AnyAsync(m => m.Id == matchId, cancellationToken);
}
