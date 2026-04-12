using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Clubs;
using NoMoreBets.Application.Common.Dto.Leagues;
using NoMoreBets.Application.Leagues;
using NoMoreBets.Application.Matches;

namespace NoMoreBets.Controllers;

/// <summary>Live FotMob scraping (Premier League table, xG, club overview, match details). Heavy; use sparingly.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class FotmobController(
  ILeagueProvider leagueProvider,
  IClubOverviewProvider clubOverviewProvider,
  IMatchDetailsProvider matchDetailsProvider) : ControllerBase
{
  /// <summary>Current league table from FotMob for the configured league.</summary>
  [HttpGet("league-table")]
  public async Task<ActionResult<IReadOnlyList<TableEntry>>> GetLeagueTable(CancellationToken cancellationToken = default)
  {
    var result = await leagueProvider.GetLeagueTableAsync(cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  /// <summary>xG table from FotMob for the configured league.</summary>
  [HttpGet("xg-stats")]
  public async Task<ActionResult<IReadOnlyList<XgStats>>> GetXgStats(CancellationToken cancellationToken = default)
  {
    var result = await leagueProvider.GetXgStatsAsync(cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  /// <summary>Team page overview (recent games, daily summary) for a FotMob team id.</summary>
  [HttpGet("clubs/{teamId:int}/overview")]
  public async Task<ActionResult<ClubOverview>> GetClubOverview(int teamId, CancellationToken cancellationToken = default)
  {
    var result = await clubOverviewProvider.GetClubOverviewAsync(teamId, cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  /// <summary>Match details (and statistics tab when available) for a full FotMob match URL.</summary>
  [HttpGet("match-details")]
  public async Task<ActionResult<MatchDetailsDto>> GetMatchDetails(
    [FromQuery] string gameUrl,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(gameUrl))
      return BadRequest("Query parameter gameUrl is required.");

    if (!Uri.TryCreate(gameUrl.Trim(), UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
      return BadRequest("gameUrl must be an absolute http(s) URL.");

    var result = await matchDetailsProvider.GetMatchDetailsAsync(uri.ToString(), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }
}
