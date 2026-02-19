using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Features.Betclic.GetBetclicMatchEvents;
using NoMoreBets.Features.Betclic.GetBetclicMatchEvents.Dtos;
using NoMoreBets.Features.Betclic.GetBetclicUpcomingGames;
using NoMoreBets.Features.Betclic.GetBetclicUpcomingGames.Dtos;
using NoMoreBets.Features.Fotmob.GetFotmobClubOverview;
using NoMoreBets.Features.Fotmob.GetFotmobClubOverview.Dtos;
using NoMoreBets.Features.Fotmob.GetFotmobClubRollingForm;
using NoMoreBets.Features.Fotmob.GetFotmobClubRollingForm.Dtos;
using NoMoreBets.Features.Fotmob.GetFotmobCoreMatchDetails;
using NoMoreBets.Features.Fotmob.GetFotmobCoreMatchDetails.Dtos;
using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable;
using NoMoreBets.Features.Fotmob.GetFotmobMatchDetails;
using NoMoreBets.Features.Fotmob.GetFotmobMatchDetails.Dtos;
using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable.Dtos;
using NoMoreBets.Features.Fotmob.GetFotmobXgStats;
using NoMoreBets.Features.Fotmob.GetFotmobXgStats.Dtos;
using NoMoreBets.Features.Fotmob.RefreshLeagueTableSnapshot;
using NoMoreBets.Features.Fotmob.Scraping;
using NoMoreBets.Features.Fotmob.UpdateFotmobRecentMatches;
using NoMoreBets.Features.Rotowire.GetRotowireLineups;
using NoMoreBets.Features.Rotowire.GetRotowireLineups.Dtos;
using NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;
using NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;
using NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;
using NoMoreBets.Features.SoccerData.GetSoccerDataMatches;
using NoMoreBets.Features.SoccerData.Model;
using NoMoreBets.Features.SoccerData;
using NoMoreBets.Features.MatchAnalysis.Model;
using NoMoreBets.Features.MatchAnalysis.RunMatchAnalysis;
using NoMoreBets.Features.Prediction.PredictMatch;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RotowireController(IMediator mediator) : ControllerBase
{
  /// <summary>
  /// Gets soccer lineups from RotoWire (games with team lineups, injuries).
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of game lineups.</returns>
  [HttpGet("rotowire/lineups")]
  public async Task<ActionResult<IReadOnlyList<GameLineupDto>>> GetRotowireLineup(
    [FromQuery] int soccerdataMatchId,
    CancellationToken cancellationToken)
  {
    await mediator.Send(new RefreshRotowireLineupsCommand(), cancellationToken);
    var lineup = await mediator.Send(new GetRotowireLineupQuery(soccerdataMatchId), cancellationToken);
    if (lineup == null)
    {
      return NotFound();
    }
    return Ok(GameLineupDto.From(lineup));
  }
}

[ApiController]
[Route("api/[controller]")]
public class BetclicController(IMediator mediator) : ControllerBase
{

  /// <summary>
  /// Gets upcoming Premier League games from Betclic.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of upcoming games with teams, time, odds, and URL.</returns>
  [HttpGet("betclic/upcoming-games")]
  public async Task<ActionResult<IReadOnlyList<UpcomingGameDto>>> GetBetclicUpcomingGames(CancellationToken cancellationToken)
  {
    var games = await mediator.Send(new GetBetclicUpcomingGamesQuery(), cancellationToken);
    return Ok(games);
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
}

[ApiController]
[Route("api/[controller]")]
public class FotmobController(IMediator mediator) : ControllerBase
{
  /// <summary>
  /// Refreshes the league table snapshot from FotMob (scrape table + xG, merge, persist) for the latest season of the given league.
  /// </summary>
  /// <param name="leagueId">Domain league ID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>204 No Content on success.</returns>
  [HttpPost("fotmob/refresh-league-table-snapshot")]
  public async Task<IActionResult> RefreshFotmobLeagueTableSnapshot(
    [FromQuery] int leagueId,
    CancellationToken cancellationToken = default)
  {
    await mediator.Send(new RefreshFotmobLeagueTableSnapshotCommand(leagueId), cancellationToken);
    return NoContent();
  }

  /// <summary>
  /// Updates recent match details from a club's FotMob overview: fetches overview, scrapes details for new match URLs, fuzzy-matches to domain Match, and inserts MatchDetails.
  /// </summary>
  /// <param name="teamId">FotMob team ID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>204 No Content on success.</returns>
  [HttpPost("fotmob/update-recent-matches")]
  public async Task<IActionResult> UpdateFotmobRecentMatches(
    [FromQuery] int teamId,
    CancellationToken cancellationToken = default)
  {
    await mediator.Send(new UpdateFotmobRecentMatchesCommand(teamId), cancellationToken);
    return NoContent();
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
  /// Gets club overview (recent games and daily summary) for a team from FotMob team overview page.
  /// </summary>
  /// <param name="teamId">FotMob team ID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Club overview with RecentGames and DailySummary.</returns>
  [HttpGet("fotmob/club-overview")]
  public async Task<ActionResult<ClubOverviewDto>> GetFotmobClubOverview(
    [FromQuery] int teamId = 10261,
    CancellationToken cancellationToken = default)
  {
    var dtos = await mediator.Send(new GetFotmobClubOverviewQuery(teamId), cancellationToken);
    return Ok(dtos);
  }

  /// <summary>
  /// Gets match details from a FotMob match detail page.
  /// </summary>
  /// <param name="gameUrl">FotMob match page URL.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Matches details with home/away teams, match date, and lineups when present.</returns>
  [HttpGet("fotmob/match-details")]
  public async Task<ActionResult<MatchDetailsDto>> GetFotmobMatchDetails(
    [FromQuery] string gameUrl,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(gameUrl))
      return BadRequest("gameUrl is required.");
    var dto = await mediator.Send(new GetFotmobMatchDetailsQuery(gameUrl), cancellationToken);
    return Ok(dto);
  }

  /// <summary>
  /// Gets core match details (goal-format per-team stats) for a team from a FotMob match page.
  /// </summary>
  /// <param name="gameUrl">FotMob match page URL.</param>
  /// <param name="teamName">Team name as used on match pages (e.g. "Paris Saint-Germain").</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Core match stats for the team, or 404 if the team is not in the match.</returns>
  [HttpGet("fotmob/core-match-details")]
  public async Task<ActionResult<GoalTeamMatchData>> GetFotmobCoreMatchDetails(
    [FromQuery] string gameUrl,
    [FromQuery] string teamName,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(gameUrl))
      return BadRequest("gameUrl is required.");
    if (string.IsNullOrWhiteSpace(teamName))
      return BadRequest("teamName is required.");
    var dto = await mediator.Send(new GetFotmobCoreMatchDetailsQuery(gameUrl, teamName), cancellationToken);
    return dto is null ? NotFound() : Ok(dto);
  }

  /// <summary>
  /// Gets rolling form (averages over last 5 games) for a club from FotMob.
  /// </summary>
  /// <param name="teamId">FotMob team ID.</param>
  /// <param name="teamName">Team name as used on match pages (e.g. "Paris Saint-Germain").</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Rolling form with averages and the list of match details used.</returns>
  [HttpGet("fotmob/club-rolling-form")]
  public async Task<ActionResult<ClubRollingFormDto>> GetFotmobClubRollingForm(
    [FromQuery] int teamId,
    [FromQuery] string teamName,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(teamName))
      return BadRequest("teamName is required.");
    var dto = await mediator.Send(new GetFotmobClubRollingFormQuery(teamId, teamName), cancellationToken);
    return Ok(dto);
  }
}

[ApiController]
[Route("api/[controller]")]
public class SoccerdataController(IMediator mediator, Initialize initialize) : ControllerBase
{
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
    await mediator.Send(new RefreshSoccerDataMatchPreviewsUpcomingCommand(leagueId), cancellationToken);
    var result = await mediator.Send(new GetSoccerDataMatchPreviewsUpcomingQuery(leagueId), cancellationToken);
    return result is null ? NotFound() : Ok(result);
  }

  /// <summary>
  /// Gets match preview for a single match from SoccerData API.
  /// </summary>
  /// <param name="matchId">Matches ID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Matches preview with teams, weather, and content.</returns>
  [HttpGet("soccerdata/match-preview")]
  public async Task<ActionResult<MatchPreview>> GetSoccerDataMatchPreview(
    [FromQuery] int matchId,
    CancellationToken cancellationToken = default)
  {
    await mediator.Send(new RefreshSoccerDataMatchPreviewCommand(matchId), cancellationToken);
    var result = await mediator.Send(new GetSoccerDataMatchPreviewQuery(matchId), cancellationToken);
    return result is null ? NotFound() : Ok(result);
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
    await mediator.Send(new RefreshSoccerDataHeadToHeadCommand(team1Id, team2Id), cancellationToken);
    var result = await mediator.Send(new GetSoccerDataHeadToHeadQuery(team1Id, team2Id), cancellationToken);
    return result is null ? NotFound() : Ok(result);
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
    var result = await mediator.Send(new GetSoccerDataMatchesQuery(date, leagueId, season), cancellationToken);
    await initialize.SeedMatchData(result);
    return Ok(result);
  }
}

[ApiController]
[Route("api/[controller]")]
public class AnalysisController(IMediator mediator) : ControllerBase
{
  /// <summary>
  /// Runs full match analysis for upcoming Betclic games (lineups, SoccerData previews, FotMob table, betting events).
  /// </summary>
  /// <param name="leagueId">Optional league ID for SoccerData upcoming previews. Default: Premier League (228).</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of match analysis results.</returns>
  [HttpGet("match-analysis")]
  public async Task<ActionResult<IReadOnlyList<MatchAnalysis>>> GetMatchAnalysis(
    [FromQuery] int? leagueId = SoccerDataConstants.PremierLeagueId,
    CancellationToken cancellationToken = default)
  {
    var result = await mediator.Send(new RunMatchAnalysisQuery(leagueId), cancellationToken);
    return Ok(result);
  }

  /// <summary>
  /// Runs multi-agent prediction for a specific match and returns a JSON betting ticket.
  /// </summary>
  /// <param name="query">PredictMatch input payload.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Prediction ticket and transcript metadata.</returns>
  [HttpPost("predict-match")]
  public async Task<ActionResult<PredictMatchResult>> PredictMatch(
    [FromBody] PredictMatchQuery query,
    CancellationToken cancellationToken = default)
  {
    var result = await mediator.Send(query, cancellationToken);
    return Ok(result);
  }
}
