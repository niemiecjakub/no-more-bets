using System.Linq;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Features.Betclic.GetBetclicMatchEvents;
using NoMoreBets.Features.Betclic.GetBetclicMatchEvents.Dtos;
using NoMoreBets.Features.Betclic.GetBetclicUpcomingGames;
using NoMoreBets.Features.Betclic.GetBetclicUpcomingGames.Dtos;
using NoMoreBets.Features.Rotowire.GetRotowireLineups;
using NoMoreBets.Features.Rotowire.GetRotowireLineups.Dtos;

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
}
