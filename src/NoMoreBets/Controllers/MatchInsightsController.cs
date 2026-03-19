using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Infrastructure.AI.Plugins;
using NoMoreBets.Infrastructure.AI.Plugins.Models;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchInsightsController(
  IPluginFactory pluginFactory,
  IUnitOfWork unitOfWork,
  AppDbContext db) : ControllerBase
{
  [HttpGet("matches/{matchId:int}/lineups")]
  public async Task<ActionResult<MatchLineupResult?>> GetLineups(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var plugin = CreatePlugin(matchId);
    var result = await plugin.GetLineupsAsync(cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matches/{matchId:int}/injuries")]
  public async Task<ActionResult<MatchInjuriesResult?>> GetInjuries(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var plugin = CreatePlugin(matchId);
    var result = await plugin.GetInjuriesAsync(cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matches/{matchId:int}/preview")]
  public async Task<ActionResult<string?>> GetPreview(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var plugin = CreatePlugin(matchId);
    var result = await plugin.GetMatchPreviewAsync(cancellationToken).ConfigureAwait(false);
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

    var plugin = CreatePlugin(matchId);
    var home = await plugin.GetClubRecentGamesAsync(match.HomeClubId, cancellationToken).ConfigureAwait(false);
    var away = await plugin.GetClubRecentGamesAsync(match.AwayClubId, cancellationToken).ConfigureAwait(false);
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

    var plugin = CreatePlugin(matchId);
    var home = await plugin.GetClubStatistics(match.HomeClubId, cancellationToken).ConfigureAwait(false);
    var away = await plugin.GetClubStatistics(match.AwayClubId, cancellationToken).ConfigureAwait(false);
    return Ok(new ClubPairDto<ClubLeagueStats?>(home, away));
  }

  [HttpGet("matches/{matchId:int}/head-to-head")]
  public async Task<ActionResult<H2H?>> GetHeadToHead(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var plugin = CreatePlugin(matchId);
    var result = await plugin.GetHead2HeadStatsAsync(cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matches/{matchId:int}/betting-odds-history")]
  public async Task<ActionResult<IReadOnlyList<MarketPriceHistory>?>> GetBettingOddsHistory(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var plugin = CreatePlugin(matchId);
    var result = await plugin.GetMatchBettingOddsHistoryAsync(cancellationToken).ConfigureAwait(false);
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

    var plugin = CreatePlugin(matchId);
    var home = await plugin.GetClubRollingPerformanceAsync(match.HomeClubId, cancellationToken).ConfigureAwait(false);
    var away = await plugin.GetClubRollingPerformanceAsync(match.AwayClubId, cancellationToken).ConfigureAwait(false);
    return Ok(new ClubPairDto<TeamPerformanceResult?>(home, away));
  }

  private MatchPlugin CreatePlugin(int matchId) => (MatchPlugin)pluginFactory.CreateMatchPlugin(matchId);

  private Task<bool> MatchExists(int matchId, CancellationToken cancellationToken) =>
    db.Match.AnyAsync(m => m.Id == matchId, cancellationToken);

  private Task<Domain.Matches.Match?> GetMatch(int matchId, CancellationToken cancellationToken) =>
    unitOfWork.Matches.GetMatchByIdAsync(matchId, cancellationToken);
}

public record ClubPairDto<T>(T Home, T Away);
