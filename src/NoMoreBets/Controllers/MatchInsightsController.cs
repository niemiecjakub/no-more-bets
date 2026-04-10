using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Application.Betting.GetMatchBettingOddsHistory;
using NoMoreBets.Application.Clubs.GetClubRecentGames;
using NoMoreBets.Application.Clubs.GetClubRollingPerformance;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues.GetClubLeagueStatistics;
using NoMoreBets.Application.Matches.GetHeadToHeadStats;
using NoMoreBets.Application.Matches.GetMatchInjuries;
using NoMoreBets.Application.Matches.GetMatchLineups;
using NoMoreBets.Application.Matches.GetMatchAgentResearch;
using NoMoreBets.Application.Matches.GetMatchPreview;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchInsightsController(
  IMediator mediator,
  IUnitOfWork unitOfWork,
  AppDbContext db) : ControllerBase
{
  [HttpGet("matches/{matchId:int}/lineups")]
  public async Task<ActionResult<MatchLineupResult?>> GetLineups(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetMatchLineupsQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matches/{matchId:int}/injuries")]
  public async Task<ActionResult<MatchInjuriesResult?>> GetInjuries(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetMatchInjuriesQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matches/{matchId:int}/preview")]
  public async Task<ActionResult<string?>> GetPreview(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetMatchPreviewQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matches/{matchId:int}/agent-research")]
  public async Task<ActionResult<string?>> GetAgentResearch(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetMatchAgentResearchQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matches/{matchId:int}/recent-games")]
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

  [HttpGet("matches/{matchId:int}/league-statistics")]
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

  [HttpGet("matches/{matchId:int}/head-to-head")]
  public async Task<ActionResult<H2H?>> GetHeadToHead(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetHeadToHeadStatsQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matches/{matchId:int}/betting-odds-history")]
  public async Task<ActionResult<IReadOnlyList<MarketPriceHistory>?>> GetBettingOddsHistory(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetMatchBettingOddsHistoryQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matches/{matchId:int}/rolling-performance")]
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

  private Task<bool> MatchExists(int matchId, CancellationToken cancellationToken) =>
    db.Match.AnyAsync(m => m.Id == matchId, cancellationToken);

  private Task<Domain.Matches.Match?> GetMatch(int matchId, CancellationToken cancellationToken) =>
    unitOfWork.Matches.GetMatchByIdAsync(matchId, cancellationToken);
}

public record ClubPairDto<T>(T Home, T Away);
