using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Clubs.Common;
using NoMoreBets.Application.Clubs.GetClubRecentGames;
using NoMoreBets.Application.Clubs.GetClubsList;
using NoMoreBets.Application.Clubs.GetClubRollingPerformance;
using NoMoreBets.Application.Clubs.GetMatchLeagueStatisticsPair;
using NoMoreBets.Application.Clubs.GetMatchRecentGamesPair;
using NoMoreBets.Application.Clubs.GetMatchRollingPerformancePair;
using NoMoreBets.Domain.Clubs;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api")]
public class ClubsController(IMediator mediator) : ControllerBase
{
  [HttpGet("clubs")]
  public async Task<ActionResult<IReadOnlyList<ClubDto>>> GetClubs(CancellationToken cancellationToken = default)
  {
    var list = await mediator.Send(new GetClubsListQuery(), cancellationToken).ConfigureAwait(false);
    return Ok(list);
  }

  [HttpGet("matchinsights/matches/{matchId:int}/recent-games")]
  public async Task<ActionResult<ClubPairDto<IReadOnlyList<RecentMatch>?>>> GetRecentGames(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    var result = await mediator
      .Send(new GetMatchRecentGamesPairQuery(matchId), cancellationToken)
      .ConfigureAwait(false);
    if (result == null)
      return NotFound();
    return Ok(result);
  }

  [HttpGet("matchinsights/matches/{matchId:int}/league-statistics")]
  public async Task<ActionResult<ClubPairDto<ClubLeagueStats?>>> GetLeagueStatistics(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    var result = await mediator
      .Send(new GetMatchLeagueStatisticsPairQuery(matchId), cancellationToken)
      .ConfigureAwait(false);
    if (result == null)
      return NotFound();
    return Ok(result);
  }

  [HttpGet("matchinsights/matches/{matchId:int}/rolling-performance")]
  public async Task<ActionResult<ClubPairDto<TeamPerformanceResult?>>> GetRollingPerformance(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    var result = await mediator
      .Send(new GetMatchRollingPerformancePairQuery(matchId), cancellationToken)
      .ConfigureAwait(false);
    if (result == null)
      return NotFound();
    return Ok(result);
  }
}
