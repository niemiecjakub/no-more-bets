using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Application.Clubs.GetClubRecentGames;
using NoMoreBets.Application.Clubs.GetClubRollingPerformance;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues.GetClubLeagueStatistics;
using NoMoreBets.Controllers.Models;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api")]
public class ClubsController(
  IMediator mediator,
  IUnitOfWork unitOfWork,
  AppDbContext db) : ControllerBase
{
  [HttpGet("clubs")]
  public async Task<ActionResult<IReadOnlyList<ClubDto>>> GetClubs(CancellationToken cancellationToken = default)
  {
    var list = await db.Club
      .Include(c => c.League)
      .OrderBy(c => c.Name)
      .Select(c => new ClubDto(c.Id, c.Name, c.LeagueId, c.League.Name, c.Slug, c.League.Slug))
      .ToListAsync(cancellationToken);
    return Ok(list);
  }

  [HttpGet("matchinsights/matches/{matchId:int}/recent-games")]
  public async Task<ActionResult<ClubPairDto<IReadOnlyList<RecentMatch>?>>> GetRecentGames(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    var match = await GetMatch(matchId, cancellationToken).ConfigureAwait(false);
    if (match == null)
      return NotFound();

    var home = await mediator.Send(new GetClubRecentGamesQuery(match.HomeClubId), cancellationToken).ConfigureAwait(false);
    var away = await mediator.Send(new GetClubRecentGamesQuery(match.AwayClubId), cancellationToken).ConfigureAwait(false);
    return Ok(new ClubPairDto<IReadOnlyList<RecentMatch>?>(home, away));
  }

  [HttpGet("matchinsights/matches/{matchId:int}/league-statistics")]
  public async Task<ActionResult<ClubPairDto<ClubLeagueStats?>>> GetLeagueStatistics(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    var match = await GetMatch(matchId, cancellationToken).ConfigureAwait(false);
    if (match == null)
      return NotFound();

    var home = await mediator.Send(new GetClubLeagueStatisticsQuery(match.HomeClubId), cancellationToken).ConfigureAwait(false);
    var away = await mediator.Send(new GetClubLeagueStatisticsQuery(match.AwayClubId), cancellationToken).ConfigureAwait(false);
    return Ok(new ClubPairDto<ClubLeagueStats?>(home, away));
  }

  [HttpGet("matchinsights/matches/{matchId:int}/rolling-performance")]
  public async Task<ActionResult<ClubPairDto<TeamPerformanceResult?>>> GetRollingPerformance(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    var match = await GetMatch(matchId, cancellationToken).ConfigureAwait(false);
    if (match == null)
      return NotFound();

    var asOfDate = DateOnly.FromDateTime(match.MatchDate);
    var home = await mediator.Send(new GetClubRollingPerformanceQuery(match.HomeClubId, asOfDate), cancellationToken).ConfigureAwait(false);
    var away = await mediator.Send(new GetClubRollingPerformanceQuery(match.AwayClubId, asOfDate), cancellationToken).ConfigureAwait(false);
    return Ok(new ClubPairDto<TeamPerformanceResult?>(home, away));
  }

  private Task<Domain.Matches.Match?> GetMatch(int matchId, CancellationToken cancellationToken) =>
    unitOfWork.Matches.GetMatchByIdAsync(matchId, cancellationToken);
}
