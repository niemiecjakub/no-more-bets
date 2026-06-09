using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Feedback.SubmitFeedback;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api")]
public class FeedbackController(IMediator mediator, ILogger<FeedbackController> logger) : ControllerBase
{
  [HttpPost("feedback")]
  public async Task<ActionResult<SubmitFeedbackResponse>> SubmitFeedback(
    [FromBody] SubmitFeedbackRequest request,
    CancellationToken cancellationToken)
  {
    if (request is null)
    {
      return BadRequest("Request body is required.");
    }

    try
    {
      var id = await mediator.Send(
        new SubmitFeedbackCommand(request.Message, request.Name, request.Email),
        cancellationToken).ConfigureAwait(false);

      return Created($"/api/feedback/{id}", new SubmitFeedbackResponse(id));
    }
    catch (ArgumentException ex)
    {
      logger.LogInformation(
        ex,
        "Feedback validation failed from {RemoteIp}",
        HttpContext.Connection.RemoteIpAddress);
      return BadRequest(ex.Message);
    }
  }
}

public sealed record SubmitFeedbackRequest(string Message, string? Name, string? Email);

public sealed record SubmitFeedbackResponse(int Id);
