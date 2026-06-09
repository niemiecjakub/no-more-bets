using MediatR;
using Microsoft.AspNetCore.Mvc;
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
    CancellationToken cancellationToken = default)
  {
    var table = await mediator
      .Send(new GetLeagueTableDisplayQuery(leagueId), cancellationToken)
      .ConfigureAwait(false);

    if (table == null)
      return NotFound();

    return Ok(table);
  }
}
