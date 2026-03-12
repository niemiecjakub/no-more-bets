using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Application.Matches.GetMatchPrediction;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Controllers;

/// <summary>
/// Endpoints for AI-powered match prediction.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PredictionController(IMediator mediator, AppDbContext db) : ControllerBase
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

  /// <summary>
  /// Triggers AI match prediction for the specified match. Runs multiple prompts, persists analyses to the database.
  /// </summary>
  /// <param name="matchId">Match ID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>204 No Content on success, 404 if match not found.</returns>
  [HttpPost("match/{matchId:int}/predict")]
  public async Task<ActionResult> RunMatchPrediction(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    var exists = await db.Match.AnyAsync(m => m.Id == matchId, cancellationToken);
    if (!exists)
      return NotFound();

    await mediator.Send(new GetMatchPredictionCommand(matchId), cancellationToken).ConfigureAwait(false);
    return NoContent();
  }
}
