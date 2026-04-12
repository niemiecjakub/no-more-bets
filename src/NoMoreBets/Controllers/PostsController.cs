using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.SocialMedia;
using NoMoreBets.Application.SocialMedia.CreateXPost;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/x/posts")]
public sealed class PostsController : ControllerBase
{
  private const int MaxPostTextLength = 4000;
  private readonly IMediator _mediator;

  public PostsController(IMediator mediator)
  {
    _mediator = mediator;
  }

  [HttpPost]
  public async Task<ActionResult<CreateXPostResult>> Create(
    [FromBody] CreateXPostRequest? body,
    CancellationToken cancellationToken)
  {
    if (body is null)
      return BadRequest("Request body is required.");

    if (string.IsNullOrWhiteSpace(body.Text))
      return BadRequest("Text is required.");

    if (body.Text.Length > MaxPostTextLength)
      return BadRequest($"Text must be at most {MaxPostTextLength} characters.");

    try
    {
      var result = await _mediator.Send(new CreateXPostCommand(body), cancellationToken);
      return Created($"/api/x/posts/{result.Id}", result);
    }
    catch (XApiPostsException ex)
    {
      return StatusCode(ex.StatusCode, new ProblemDetails
      {
        Title = "X API error",
        Detail = ex.Message,
        Status = ex.StatusCode
      });
    }
    catch (InvalidOperationException ex)
    {
      return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
      {
        Title = "X API is not configured",
        Detail = ex.Message,
        Status = StatusCodes.Status503ServiceUnavailable
      });
    }
  }
}
