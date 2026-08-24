using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.Bankroll.GetBankrollBettingBalance;
using NoMoreBets.Application.Bankroll.GetBankrollDashboard;
using NoMoreBets.Application.Bankroll.GetBankrollEntriesPage;
using NoMoreBets.Application.Bankroll.GetBankrollEntryBetDetails;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Bankrolls;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api")]
public class BankrollController(IMediator mediator) : ControllerBase
{
  [HttpGet("bankroll")]
  public async Task<ActionResult<BankrollDashboardDto>> GetBankrollDashboard(
    CancellationToken cancellationToken = default)
  {
    var result = await mediator.Send(new GetBankrollDashboardQuery(), cancellationToken);
    return Ok(result);
  }

  [HttpGet("bankroll/betting-balance")]
  public async Task<ActionResult<BankrollBettingBalanceDto>> GetBettingBalance(
    CancellationToken cancellationToken = default)
  {
    var result = await mediator.Send(new GetBankrollBettingBalanceQuery(), cancellationToken);
    return Ok(result);
  }

  [HttpGet("bankroll/entries")]
  public async Task<ActionResult<Paged<BankrollEntryListItemDto>>> GetEntries(
    [FromQuery] int limit = 15,
    [FromQuery] DateTime? afterCreatedAt = null,
    [FromQuery] int? afterId = null,
    [FromQuery] string[]? entryNames = null,
    [FromQuery] string[]? seasonYears = null,
    CancellationToken cancellationToken = default)
  {
    limit = Math.Clamp(limit, 1, 100);

    if (afterCreatedAt is null != afterId is null)
    {
      return BadRequest("afterCreatedAt and afterId must both be provided or omitted.");
    }

    IReadOnlyCollection<string>? entryNameFilter = null;
    if (entryNames is { Length: > 0 })
    {
      var parsedNames = new List<string>(entryNames.Length);
      foreach (var entryName in entryNames)
      {
        if (!BankrollEntryNames.All.Contains(entryName))
          return BadRequest($"Invalid entryNames value: {entryName}.");

        parsedNames.Add(entryName);
      }

      entryNameFilter = parsedNames;
    }

    DateTime? afterCreatedAtUtc = afterCreatedAt is not null
      ? UtcDateTime.ToUtc(afterCreatedAt.Value)
      : null;

    var result = await mediator.Send(
      new GetBankrollEntriesPageQuery(
        limit,
        afterCreatedAtUtc,
        afterId,
        entryNameFilter,
        SeasonYearQueryExtensions.Normalize(seasonYears)),
      cancellationToken);

    return Ok(result);
  }

  [HttpGet("bankroll/entries/{entryId:int}/bet-details")]
  public async Task<ActionResult<BankrollEntryBetDetailsDto>> GetEntryBetDetails(
    int entryId,
    CancellationToken cancellationToken = default)
  {
    var result = await mediator.Send(new GetBankrollEntryBetDetailsQuery(entryId), cancellationToken);
    if (result is null)
      return NotFound();

    return Ok(result);
  }
}
