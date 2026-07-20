using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Betting.UpdateMatches;
using NoMoreBets.Application.Leagues.GetLeagueTableDisplay;
using NoMoreBets.Application.Leagues.GetLeaguesList;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api")]
public class LeaguesController(IMediator mediator) : ControllerBase
{
  [HttpGet("leagues")]
  public async Task<ActionResult<IReadOnlyList<LeagueDto>>> GetLeagues(CancellationToken cancellationToken = default)
  {
    var list = await mediator.Send(new GetLeaguesListQuery(), cancellationToken).ConfigureAwait(false);
    return Ok(list);
  }

  [HttpGet("leagues/{leagueId:int}/table")]
  public async Task<ActionResult<LeagueTableDto>> GetLeagueTable(
    int leagueId,
    [FromQuery] int seasonId,
    [FromQuery] int? clubId,
    CancellationToken cancellationToken = default)
  {
    var table = await mediator
      .Send(new GetLeagueTableDisplayQuery(leagueId, seasonId, clubId), cancellationToken)
      .ConfigureAwait(false);

    if (table == null)
      return NotFound();

    return Ok(table);
  }

  /// <summary>Fetches upcoming Betclic games for the league and adds matches that do not exist yet.</summary>
  [HttpPost("leagues/{leagueId:int}/update-matches")]
  public async Task<ActionResult<UpdateMatchesResultDto>> UpdateMatches(
    int leagueId,
    CancellationToken cancellationToken = default)
  {
    var leagues = await mediator.Send(new GetLeaguesListQuery(), cancellationToken).ConfigureAwait(false);
    if (leagues.All(l => l.Id != leagueId))
      return NotFound();

    var added = await mediator.Send(new UpdateMatchesCommand(leagueId), cancellationToken).ConfigureAwait(false);

    return Ok(new UpdateMatchesResultDto(
      added.Count,
      added
        .Select(m => new AddedMatchDto(m.Id, m.MatchDate, m.HomeClubId, m.AwayClubId, m.BetclicUrl))
        .ToList()));
  }
}

public sealed record AddedMatchDto(
  int Id,
  DateTime MatchDate,
  int HomeClubId,
  int AwayClubId,
  string? BetclicUrl);

public sealed record UpdateMatchesResultDto(int AddedCount, IReadOnlyList<AddedMatchDto> AddedMatches);
