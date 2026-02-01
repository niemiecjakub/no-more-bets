using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Features.Betclic.GetBetclicMatchEvents;
using NoMoreBets.Features.Betclic.GetBetclicMatchEvents.Dtos;
using NoMoreBets.Features.Betclic.GetBetclicUpcomingGames;
using NoMoreBets.Features.Betclic.GetBetclicUpcomingGames.Dtos;
using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable;
using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable.Dtos;
using NoMoreBets.Features.Fotmob.GetFotmobXgStats;
using NoMoreBets.Features.Fotmob.GetFotmobXgStats.Dtos;
using NoMoreBets.Features.Fotmob.Scraping;
using NoMoreBets.Features.Rotowire.GetRotowireLineups;
using NoMoreBets.Features.Rotowire.GetRotowireLineups.Dtos;
using NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;
using NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;
using NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;
using NoMoreBets.Features.SoccerData.GetSoccerDataMatches;
using NoMoreBets.Features.SoccerData.Model;
using NoMoreBets.Features.SoccerData;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScraperController(IMediator mediator) : ControllerBase
{
  /// <summary>
  /// Gets soccer lineups from RotoWire (games with team lineups, injuries).
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of game lineups.</returns>
  [HttpGet("rotowire/lineups")]
  public async Task<ActionResult<IReadOnlyList<GameLineupDto>>> GetRotowireLineups(CancellationToken cancellationToken)
  {
    var lineups = await mediator.Send(new GetRotowireLineupsQuery(), cancellationToken);
    var lineupsDto = lineups.Select(GameLineupDto.From).ToList();
    return Ok(lineupsDto);
  }

  /// <summary>
  /// Gets upcoming Premier League games from Betclic.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of upcoming games with teams, time, odds, and URL.</returns>
  [HttpGet("betclic/upcoming-games")]
  public async Task<ActionResult<IReadOnlyList<UpcomingGameDto>>> GetBetclicUpcomingGames(CancellationToken cancellationToken)
  {
    var games = await mediator.Send(new GetBetclicUpcomingGamesQuery(), cancellationToken);
    var dtos = games.Select(UpcomingGameDto.From).ToList();
    return Ok(dtos);
  }

  /// <summary>
  /// Gets bookmaker events (markets) for a specific match from Betclic.
  /// </summary>
  /// <param name="gameUrl">URL to the match page.</param>
  /// <param name="expand">If true, clicks consent/modal and "see more" before parsing. Default false.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of bookmaker events with title and options.</returns>
  [HttpGet("betclic/match-events")]
  public async Task<ActionResult<IReadOnlyList<BookmakerEventDto>>> GetBetclicMatchEvents(
    [FromQuery] string gameUrl,
    [FromQuery] bool expand = false,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(gameUrl))
      return BadRequest("gameUrl is required.");
    var events = await mediator.Send(new GetBetclicMatchEventsQuery(gameUrl, expand), cancellationToken);
    var dtos = events.Select(BookmakerEventDto.From).ToList();
    return Ok(dtos);
  }

  /// <summary>
  /// Gets the league table from FotMob (Premier League by default), optionally filtered by home/away/form.
  /// </summary>
  /// <param name="filter">Table filter: all, home, away, form. Default all.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of clubs in table order.</returns>
  [HttpGet("fotmob/league-table")]
  public async Task<ActionResult<IReadOnlyList<ClubDto>>> GetFotmobLeagueTable(
    [FromQuery] TableFilter filter = TableFilter.All,
    CancellationToken cancellationToken = default)
  {
    var dtos = await mediator.Send(new GetFotmobLeagueTableQuery(filter), cancellationToken);
    return Ok(dtos);
  }

  /// <summary>
  /// Gets xG statistics table from FotMob for the configured league.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of xG stats per team.</returns>
  [HttpGet("fotmob/xg-stats")]
  public async Task<ActionResult<IReadOnlyList<XgStatsDto>>> GetFotmobXgStats(CancellationToken cancellationToken)
  {
    var dtos = await mediator.Send(new GetFotmobXgStatsQuery(), cancellationToken);
    return Ok(dtos);
  }

  /// <summary>
  /// Gets upcoming match previews from SoccerData API, optionally filtered by league ID.
  /// Defaults to Premier League when no league ID is provided.
  /// </summary>
  /// <param name="leagueId">Optional league ID to filter results. Default: Premier League (228).</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of league match previews.</returns>
  [HttpGet("soccerdata/match-previews-upcoming")]
  public async Task<ActionResult<IReadOnlyList<LeagueMatchPreviews>>> GetSoccerDataMatchPreviewsUpcoming(
    [FromQuery] int? leagueId = SoccerDataConstants.PremierLeagueId,
    CancellationToken cancellationToken = default)
  {
    var effectiveLeagueId = leagueId;
    var result = await mediator.Send(new GetSoccerDataMatchPreviewsUpcomingQuery(effectiveLeagueId), cancellationToken);
    return Ok(result);
  }

  /// <summary>
  /// Gets match preview for a single match from SoccerData API.
  /// </summary>
  /// <param name="matchId">Match ID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Match preview with teams, weather, and content.</returns>
  [HttpGet("soccerdata/match-preview")]
  public async Task<ActionResult<MatchPreview>> GetSoccerDataMatchPreview(
    [FromQuery] int matchId,
    CancellationToken cancellationToken = default)
  {
    var result = await mediator.Send(new GetSoccerDataMatchPreviewQuery(matchId), cancellationToken);
    return Ok(result);
  }

  /// <summary>
  /// Gets head-to-head data between two teams from SoccerData API.
  /// </summary>
  /// <param name="team1Id">First team ID.</param>
  /// <param name="team2Id">Second team ID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Head-to-head stats and team info.</returns>
  [HttpGet("soccerdata/head-to-head")]
  public async Task<ActionResult<HeadToHead>> GetSoccerDataHeadToHead(
    [FromQuery] int team1Id,
    [FromQuery] int team2Id,
    CancellationToken cancellationToken = default)
  {
    var result = await mediator.Send(new GetSoccerDataHeadToHeadQuery(team1Id, team2Id), cancellationToken);
    return Ok(result);
  }

  /// <summary>
  /// Gets matches from SoccerData API by date, league ID, and/or season.
  /// Defaults to Premier League and current season when league/season not provided.
  /// </summary>
  /// <param name="date">Optional date filter.</param>
  /// <param name="leagueId">Optional league ID. Default: Premier League (228).</param>
  /// <param name="season">Optional season (e.g. 2025-2026). Default: current season.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of league matches.</returns>
  [HttpGet("soccerdata/matches")]
  public async Task<ActionResult<IReadOnlyList<LeagueMatches>>> GetSoccerDataMatches(
    [FromQuery] string? date = null,
    [FromQuery] int? leagueId = SoccerDataConstants.PremierLeagueId,
    [FromQuery] string? season = SoccerDataConstants.CurrentSeason,
    CancellationToken cancellationToken = default)
  {
    var effectiveLeagueId = leagueId;
    var effectiveSeason = season;
    var result = await mediator.Send(new GetSoccerDataMatchesQuery(date, effectiveLeagueId, effectiveSeason), cancellationToken);
    return Ok(result);
  }
}
