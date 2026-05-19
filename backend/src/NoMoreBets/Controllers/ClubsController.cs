using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Clubs.Common;
using NoMoreBets.Application.Clubs.GetClubBetSelectionStats;
using NoMoreBets.Application.Clubs.GetClubById;
using NoMoreBets.Application.Clubs.GetClubNextMatch;
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

  [HttpGet("clubs/{clubId:int}")]
  public async Task<ActionResult<ClubDetailDto>> GetClubById(
    int clubId,
    CancellationToken cancellationToken = default)
  {
    var club = await mediator.Send(new GetClubByIdQuery(clubId), cancellationToken).ConfigureAwait(false);
    if (club == null)
      return NotFound();
    return Ok(club);
  }

  [HttpGet("clubs/{clubId:int}/recent-games")]
  public async Task<ActionResult<IReadOnlyList<RecentMatch>>> GetClubRecentGames(
    int clubId,
    CancellationToken cancellationToken = default)
  {
    var games = await mediator
      .Send(new GetClubRecentGamesQuery(clubId), cancellationToken)
      .ConfigureAwait(false);
    if (games == null)
      return NotFound();
    return Ok(games);
  }

  [HttpGet("clubs/{clubId:int}/next-match")]
  public async Task<ActionResult<ClubNextMatchDto>> GetClubNextMatch(
    int clubId,
    CancellationToken cancellationToken = default)
  {
    var nextMatch = await mediator
      .Send(new GetClubNextMatchQuery(clubId), cancellationToken)
      .ConfigureAwait(false);

    if (nextMatch == null)
    {
      var club = await mediator.Send(new GetClubByIdQuery(clubId), cancellationToken).ConfigureAwait(false);
      if (club == null)
        return NotFound();
      return NoContent();
    }

    return Ok(nextMatch);
  }

  [HttpGet("clubs/{clubId:int}/bet-selection-stats")]
  public async Task<ActionResult<ClubBetSelectionStatsDto>> GetClubBetSelectionStats(
    int clubId,
    CancellationToken cancellationToken = default)
  {
    var stats = await mediator
      .Send(new GetClubBetSelectionStatsQuery(clubId), cancellationToken)
      .ConfigureAwait(false);
    if (stats == null)
      return NotFound();
    return Ok(stats);
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
