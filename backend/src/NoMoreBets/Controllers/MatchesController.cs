using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetHeadToHeadStats;
using NoMoreBets.Application.Matches.GetMatchAnalyses;
using NoMoreBets.Application.Matches.GetMatchesPage;
using NoMoreBets.Application.Matches.GetMatchInjuries;
using NoMoreBets.Application.Matches.GetMatchLineups;
using MatchInjuriesResult = NoMoreBets.Application.Matches.GetMatchInjuries.MatchInjuriesResult;
using MatchLineupResult = NoMoreBets.Application.Matches.GetMatchLineups.MatchLineupResult;
using NoMoreBets.Application.Matches.MatchExists;
using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api")]
public class MatchesController(IMediator mediator) : ControllerBase
{
  [HttpGet("matches")]
  public async Task<ActionResult<Paged<MatchDto>>> GetMatches(
    [FromQuery] int? matchStatusId = null,
    [FromQuery] int[]? leagueIds = null,
    [FromQuery] int limit = 10,
    [FromQuery] DateTime? afterMatchDate = null,
    [FromQuery] int? afterId = null,
    CancellationToken cancellationToken = default)
  {
    limit = Math.Clamp(limit, 1, 100);

    if (afterMatchDate is null != afterId is null)
      return BadRequest("afterMatchDate and afterId must both be provided or omitted.");

    DateTime? afterMatchDateUtc = afterMatchDate is not null
      ? DateTimeQueryExtensions.ToUtc(afterMatchDate.Value)
      : null;

    var result = await mediator.Send(
      new GetMatchesPageQuery(
        limit,
        matchStatusId,
        (leagueIds ?? []).Distinct().ToArray(),
        afterMatchDateUtc,
        afterId),
      cancellationToken).ConfigureAwait(false);

    return Ok(result);
  }

  [HttpGet("matches/{matchId:int}/analyses")]
  public async Task<ActionResult<MatchAnalysisPageDto>> GetMatchAnalyses(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    var page = await mediator
      .Send(new GetMatchAnalysesQuery(matchId), cancellationToken)
      .ConfigureAwait(false);

    if (page == null)
      return NotFound();

    return Ok(page);
  }

  [HttpGet("matchinsights/matches/{matchId:int}/lineups")]
  public async Task<ActionResult<MatchLineupResult?>> GetLineups(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetMatchLineupsQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matchinsights/matches/{matchId:int}/injuries")]
  public async Task<ActionResult<MatchInjuriesResult?>> GetInjuries(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetMatchInjuriesQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matchinsights/matches/{matchId:int}/head-to-head")]
  public async Task<ActionResult<H2H?>> GetHeadToHead(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetHeadToHeadStatsQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  private Task<bool> MatchExists(int matchId, CancellationToken cancellationToken) =>
    mediator.Send(new MatchExistsQuery(matchId), cancellationToken);
}
