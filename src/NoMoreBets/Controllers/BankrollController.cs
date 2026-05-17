using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Application.Bankroll.GetBankrollDashboard;
using NoMoreBets.Application.Bankroll.GetBankrollEntriesPage;
using NoMoreBets.Controllers.Models;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api")]
public class BankrollController(IMediator mediator, AppDbContext db) : ControllerBase
{
  public record BankrollBettingBalanceDto(decimal Balance);
  public record BankrollEntryListItemDto(
    int Id,
    string Name,
    decimal Amount,
    string Flow,
    decimal Delta,
    DateTime CreatedAt,
    int? BetId);
  public record BankrollEntryBetDetailsDto(
    int EntryId,
    int BetId,
    DateTime BetCreatedAt,
    decimal StakeAmount,
    decimal TotalOdds,
    decimal PotentialPayout,
    int StatusId,
    string StatusName,
    int? AgentSessionId,
    IReadOnlyList<BetSelectionItemDto> Selections);

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
    var balance = (await db.Bankroll
      .AsNoTracking()
      .Where(record => record.BetId != null)
      .SumAsync(
        record => (decimal?)(record.Flow == BankrollFlowExtensions.InCode ? record.Amount : -record.Amount),
        cancellationToken)) ?? 0m;

    return Ok(new BankrollBettingBalanceDto(balance));
  }

  [HttpGet("bankroll/entries")]
  public async Task<ActionResult<PagedResponse<BankrollEntryListItemDto>>> GetEntries(
    [FromQuery] int limit = 15,
    [FromQuery] DateTime? afterCreatedAt = null,
    [FromQuery] int? afterId = null,
    CancellationToken cancellationToken = default)
  {
    limit = Math.Clamp(limit, 1, 100);

    if (afterCreatedAt is null != afterId is null)
    {
      return BadRequest("afterCreatedAt and afterId must both be provided or omitted.");
    }

    var query = db.Bankroll.AsNoTracking();
    if (afterCreatedAt is not null && afterId is not null)
    {
      var cursorCreatedAt = DateTimeQueryExtensions.ToUtc(afterCreatedAt.Value);
      var cursorId = afterId.Value;
      query = query.Where(row =>
        row.CreatedAt < cursorCreatedAt
        || (row.CreatedAt == cursorCreatedAt && row.Id < cursorId));
    }

    var rows = await query
      .OrderByDescending(row => row.CreatedAt)
      .ThenByDescending(row => row.Id)
      .Take(limit + 1)
      .Select(row => new BankrollEntryRow(
        row.Id,
        row.Name,
        row.Amount,
        row.Flow,
        row.CreatedAt,
        row.BetId))
      .ToListAsync(cancellationToken);

    var hasMore = rows.Count > limit;
    if (hasMore)
      rows.RemoveAt(rows.Count - 1);

    var items = BankrollEntriesPagination.MapRows(rows)
      .Select(item => new BankrollEntryListItemDto(
        item.Id,
        item.Name,
        item.Amount,
        item.Flow,
        item.Delta,
        item.CreatedAt,
        item.BetId))
      .ToList();

    return Ok(PagedResponseFactory.Create(items, hasMore, item => item.CreatedAt, item => item.Id));
  }

  [HttpGet("bankroll/entries/{entryId:int}/bet-details")]
  public async Task<ActionResult<BankrollEntryBetDetailsDto>> GetEntryBetDetails(
    int entryId,
    CancellationToken cancellationToken = default)
  {
    var entry = await db.Bankroll
      .AsNoTracking()
      .Where(row => row.Id == entryId)
      .Select(row => new { row.Id, row.BetId })
      .SingleOrDefaultAsync(cancellationToken);

    if (entry is null || entry.BetId is null)
      return NotFound();

    var bet = await db.BetSlip
      .AsNoTracking()
      .Where(slip => slip.Id == entry.BetId.Value)
      .Where(slip => slip.AgentSession != null && slip.AgentSession.Phase == AgentSessionPhase.Betting)
      .Select(slip => new
      {
        slip.Id,
        slip.CreatedAt,
        slip.StakeAmount,
        slip.TotalOdds,
        slip.PotentialPayout,
        slip.StatusId,
        StatusName = slip.BetStatusEntity.Name,
        slip.AgentSessionId,
        Selections = slip.Selections
          .OrderBy(selection => selection.Id)
          .Select(selection => new BetSelectionItemDto(
            selection.MatchId,
            selection.Match.HomeClub.Name,
            selection.Match.AwayClub.Name,
            selection.Match.HomeClub.Slug,
            selection.Match.AwayClub.Slug,
            BettingEventTypeDisplay.GetDisplayName(selection.BetEventType),
            BettingEventOptionDisplay.GetDisplayName(
              selection.BetEventOption,
              selection.Match.HomeClub.Name,
              selection.Match.AwayClub.Name),
            selection.OddsAtPlacement,
            selection.StatusId,
            selection.BetStatusEntity.Name))
          .ToList()
      })
      .SingleOrDefaultAsync(cancellationToken);

    if (bet is null)
      return NotFound();

    return Ok(new BankrollEntryBetDetailsDto(
      entry.Id,
      bet.Id,
      bet.CreatedAt,
      bet.StakeAmount,
      bet.TotalOdds,
      bet.PotentialPayout,
      bet.StatusId,
      bet.StatusName,
      bet.AgentSessionId,
      bet.Selections));
  }
}
