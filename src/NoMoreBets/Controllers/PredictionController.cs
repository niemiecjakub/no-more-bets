using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Matches.GetMatchPrediction;

namespace NoMoreBets.Controllers;

/// <summary>
/// Endpoints for AI-powered match prediction.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PredictionController(IMediator mediator) : ControllerBase
{
  /// <summary>
  /// Generates a match prediction.
  /// </summary>
  /// <param name="matchId">The internal match ID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The model's prediction text (score, reasoning, betting insight) or an error message if the match is not found.</returns>
  [HttpGet("match/{matchId:int}")]
  public async Task<ActionResult<string>> PredictMatch(int matchId, CancellationToken cancellationToken = default)
  {
    var result = await mediator.Send(new GetMatchPredictionCommand(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }
}
