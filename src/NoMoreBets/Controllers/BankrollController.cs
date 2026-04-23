using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Bankroll.GetBankrollDashboard;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/database")]
public class BankrollController(IMediator mediator) : ControllerBase
{
  [HttpGet("bankroll")]
  public async Task<ActionResult<BankrollDashboardDto>> GetBankrollDashboard(
    CancellationToken cancellationToken = default)
  {
    var result = await mediator.Send(new GetBankrollDashboardQuery(), cancellationToken);
    return Ok(result);
  }
}
