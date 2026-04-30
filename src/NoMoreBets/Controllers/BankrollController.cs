using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Application.Bankroll.GetBankrollDashboard;
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
  public record BankrollFlowPointDto(
    int EntryId,
    DateTime Timestamp,
    decimal Delta,
    decimal BalanceAfter,
    string Flow,
    int? BetId,
    string Name);
  public record BankrollEntryListItemDto(
    int Id,
    string Name,
    decimal Amount,
    string Flow,
    decimal Delta,
    DateTime CreatedAt,
    int? BetId,
    decimal BalanceAfter);
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

  [HttpGet("bankroll/flow-points")]
  public async Task<ActionResult<IReadOnlyList<BankrollFlowPointDto>>> GetFlowPoints(
    CancellationToken cancellationToken = default)
  {
    var rows = await db.Bankroll
      .AsNoTracking()
      .OrderBy(row => row.CreatedAt)
      .ThenBy(row => row.Id)
      .Select(row => new
      {
        row.Id,
        row.CreatedAt,
        row.Amount,
        row.Flow,
        row.BetId,
        row.Name
      })
      .ToListAsync(cancellationToken);

    var runningBalance = 0m;
    var points = rows
      .Select(row =>
      {
        var delta = row.Flow == BankrollFlowExtensions.InCode ? row.Amount : -row.Amount;
        runningBalance += delta;
        var flow = row.Flow == BankrollFlowExtensions.InCode ? nameof(BankrollFlow.In) : nameof(BankrollFlow.Out);
        return new BankrollFlowPointDto(row.Id, row.CreatedAt, delta, runningBalance, flow, row.BetId, row.Name);
      })
      .ToList();

    return Ok(points);
  }

  [HttpGet("bankroll/entries")]
  public async Task<ActionResult<IReadOnlyList<BankrollEntryListItemDto>>> GetEntries(
    CancellationToken cancellationToken = default)
  {
    var rows = await db.Bankroll
      .AsNoTracking()
      .OrderBy(row => row.CreatedAt)
      .ThenBy(row => row.Id)
      .Select(row => new
      {
        row.Id,
        row.Name,
        row.Amount,
        row.Flow,
        row.CreatedAt,
        row.BetId
      })
      .ToListAsync(cancellationToken);

    var runningBalance = 0m;
    var entries = rows
      .Select(row =>
      {
        var delta = row.Flow == BankrollFlowExtensions.InCode ? row.Amount : -row.Amount;
        runningBalance += delta;
        var flow = row.Flow == BankrollFlowExtensions.InCode ? nameof(BankrollFlow.In) : nameof(BankrollFlow.Out);
        return new BankrollEntryListItemDto(
          row.Id,
          row.Name,
          row.Amount,
          flow,
          delta,
          row.CreatedAt,
          row.BetId,
          runningBalance);
      })
      .OrderByDescending(row => row.CreatedAt)
      .ThenByDescending(row => row.Id)
      .ToList();

    return Ok(entries);
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
