using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Application.Bankroll.GetDaysUntilPayday;
using NoMoreBets.Controllers.Models;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/agent/dashboard")]
public class AgentDashboardController(AppDbContext db, IMediator mediator) : ControllerBase
{
  [HttpGet("bankroll")]
  public async Task<ActionResult<AgentDashboardBankrollDto>> GetBankrollWidget(
    CancellationToken cancellationToken = default)
  {
    var totalValue = (await db.Bankroll
      .AsNoTracking()
      .SumAsync(
        record => (decimal?)(record.Flow == BankrollFlowExtensions.InCode ? record.Amount : -record.Amount),
        cancellationToken)) ?? 0m;

    var balance = (await db.Bankroll
      .AsNoTracking()
      .Where(record => record.BetId != null)
      .SumAsync(
        record => (decimal?)(record.Flow == BankrollFlowExtensions.InCode ? record.Amount : -record.Amount),
        cancellationToken)) ?? 0m;

    var daysUntilPayday = await mediator
      .Send(new GetDaysUntilPaydayQuery(), cancellationToken)
      .ConfigureAwait(false);

    return Ok(new AgentDashboardBankrollDto(totalValue, balance, daysUntilPayday));
  }

  [HttpGet("betting-summary")]
  public async Task<ActionResult<AgentDashboardBettingSummaryDto>> GetBettingSummaryWidget(
    CancellationToken cancellationToken = default)
  {
    var settled = await db.BetSlip
      .AsNoTracking()
      .Where(s => s.AgentSession != null && s.AgentSession.Phase == AgentSessionPhase.Betting)
      .Where(s => s.StatusId != (int)BetStatus.Pending)
      .Select(s => new
      {
        s.StatusId,
        SelectionsCount = s.Selections.Count
      })
      .ToListAsync(cancellationToken);

    var settledCount = settled.Count;
    var wonCount = settled.Count(s => s.StatusId == (int)BetStatus.Won);
    var lostCount = settled.Count(s => s.StatusId == (int)BetStatus.Lost);
    var winRate = settledCount == 0 ? 0m : (decimal)wonCount / settledCount * 100m;
    var lossRate = settledCount == 0 ? 0m : (decimal)lostCount / settledCount * 100m;

    return Ok(new AgentDashboardBettingSummaryDto(
      SettledSlipsCount: settledCount,
      SettledSelectionsCount: settled.Sum(s => s.SelectionsCount),
      WonSlipsCount: wonCount,
      LostSlipsCount: lostCount,
      WinRatePercent: winRate,
      LossRatePercent: lossRate));
  }

  [HttpGet("research-betting-summary")]
  public async Task<ActionResult<AgentDashboardResearchBettingSummaryDto>> GetResearchBettingSummaryWidget(
    [FromQuery] int[]? leagueIds,
    [FromQuery(Name = "leagueIds[]")] int[]? leagueIdsBracket,
    CancellationToken cancellationToken = default)
  {
    var selectedLeagueIds = (leagueIds ?? [])
      .Concat(leagueIdsBracket ?? [])
      .Where(id => id > 0)
      .Distinct()
      .ToArray();

    var settledQuery = db.BetSlip
      .AsNoTracking()
      .Where(s => s.AgentSession != null && s.AgentSession.Phase == AgentSessionPhase.Research)
      .Where(s => s.StatusId != (int)BetStatus.Pending);

    if (selectedLeagueIds.Length > 0)
    {
      settledQuery = settledQuery
        .Where(s => s.Selections.Any(sel =>
          sel.Match != null &&
          sel.Match.Stage != null &&
          sel.Match.Stage.Season != null &&
          selectedLeagueIds.Contains(sel.Match.Stage.Season.LeagueId)));
    }

    var settledSelections = await settledQuery
      .SelectMany(s => s.Selections)
      .Where(sel => selectedLeagueIds.Length == 0 || (
        sel.Match != null &&
        sel.Match.Stage != null &&
        sel.Match.Stage.Season != null &&
        selectedLeagueIds.Contains(sel.Match.Stage.Season.LeagueId)))
      .Select(sel => sel.StatusId)
      .ToListAsync(cancellationToken);

    var settledSelectionsCount = settledSelections.Count;
    var wonSelectionsCount = settledSelections.Count(statusId => statusId == (int)BetStatus.Won);
    var lostSelectionsCount = settledSelections.Count(statusId => statusId == (int)BetStatus.Lost);
    var winRate = settledSelectionsCount == 0 ? 0m : (decimal)wonSelectionsCount / settledSelectionsCount * 100m;
    var lossRate = settledSelectionsCount == 0 ? 0m : (decimal)lostSelectionsCount / settledSelectionsCount * 100m;

    return Ok(new AgentDashboardResearchBettingSummaryDto(
      SettledSelectionsCount: settledSelectionsCount,
      WonSelectionsCount: wonSelectionsCount,
      LostSelectionsCount: lostSelectionsCount,
      WinRatePercent: winRate,
      LossRatePercent: lossRate));
  }

  [HttpGet("betting-summary/details")]
  public async Task<ActionResult<AgentDashboardBettingSummaryDetailsDto>> GetBettingSummaryDetails(
    CancellationToken cancellationToken = default)
  {
    var slips = await db.BetSlip
      .AsNoTracking()
      .Where(s => s.AgentSession != null && s.AgentSession.Phase == AgentSessionPhase.Betting)
      .Where(s => s.StatusId == (int)BetStatus.Won || s.StatusId == (int)BetStatus.Lost)
      .Include(s => s.BetStatusEntity)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m.AwayClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.BetStatusEntity)
      .OrderByDescending(s => s.CreatedAt)
      .ToListAsync(cancellationToken);

    var wonSlipsCount = slips.Count(s => s.StatusId == (int)BetStatus.Won);
    var lostSlipsCount = slips.Count(s => s.StatusId == (int)BetStatus.Lost);

    var wonSelectionsCount = slips
      .SelectMany(s => s.Selections)
      .Count(sel => sel.StatusId == (int)BetStatus.Won);
    var lostSelectionsCount = slips
      .SelectMany(s => s.Selections)
      .Count(sel => sel.StatusId == (int)BetStatus.Lost);

    var slipDtos = slips
      .Select(s => new BetSlipListItemDto(
        s.Id,
        s.CreatedAt,
        s.StakeAmount,
        s.TotalOdds,
        s.PotentialPayout,
        s.StatusId,
        s.BetStatusEntity.Name,
        s.Selections
          .OrderBy(sel => sel.Id)
          .Select(sel => new BetSelectionItemDto(
            sel.MatchId,
            sel.Match.HomeClub.Name,
            sel.Match.AwayClub.Name,
            sel.Match.HomeClub.Slug,
            sel.Match.AwayClub.Slug,
            BettingEventTypeDisplay.GetDisplayName(sel.BetEventType),
            BettingEventOptionDisplay.GetDisplayName(sel.BetEventOption, sel.Match.HomeClub.Name, sel.Match.AwayClub.Name),
            sel.OddsAtPlacement,
            sel.StatusId,
            sel.BetStatusEntity.Name))
          .ToList(),
        s.AgentSessionId))
      .ToList();

    return Ok(new AgentDashboardBettingSummaryDetailsDto(
      WonSlipsCount: wonSlipsCount,
      LostSlipsCount: lostSlipsCount,
      WonSelectionsCount: wonSelectionsCount,
      LostSelectionsCount: lostSelectionsCount,
      Slips: slipDtos));
  }

  [HttpGet("pending-bets")]
  public async Task<ActionResult<AgentDashboardPendingBetsDto>> GetPendingBetsWidget(
    CancellationToken cancellationToken = default)
  {
    var pending = await db.BetSlip
      .AsNoTracking()
      .Where(s => s.AgentSession != null && s.AgentSession.Phase == AgentSessionPhase.Betting)
      .Where(s => s.StatusId == (int)BetStatus.Pending)
      .Select(s => new
      {
        s.StakeAmount,
        s.PotentialPayout,
        s.CreatedAt
      })
      .ToListAsync(cancellationToken);

    return Ok(new AgentDashboardPendingBetsDto(
      PendingSlipsCount: pending.Count,
      PendingStakeTotal: pending.Sum(s => s.StakeAmount),
      PendingPotentialPayoutTotal: pending.Sum(s => s.PotentialPayout),
      LatestPendingCreatedAt: pending
        .OrderByDescending(s => s.CreatedAt)
        .Select(s => (DateTime?)s.CreatedAt)
        .FirstOrDefault()));
  }

  [HttpGet("sessions")]
  public async Task<ActionResult<AgentDashboardSessionsDto>> GetSessionsWidget(
    CancellationToken cancellationToken = default)
  {
    var sessions = await db.AgentSession
      .AsNoTracking()
      .Select(s => new
      {
        s.StartedAt,
        s.Phase
      })
      .ToListAsync(cancellationToken);

    var latestSession = sessions
      .OrderByDescending(s => s.StartedAt)
      .FirstOrDefault();

    return Ok(new AgentDashboardSessionsDto(
      SessionsCount: sessions.Count,
      LatestStartedAt: latestSession?.StartedAt,
      LatestPhaseName: latestSession?.Phase.ToString()));
  }

  [HttpGet("memories")]
  public async Task<ActionResult<AgentDashboardMemoriesDto>> GetMemoriesWidget(
    CancellationToken cancellationToken = default)
  {
    var memories = await db.Memory
      .AsNoTracking()
      .Where(m => m.DeletedAt == null)
      .Select(m => new
      {
        m.Name,
        m.UpdatedAt
      })
      .ToListAsync(cancellationToken);

    var latestMemory = memories
      .OrderByDescending(m => m.UpdatedAt)
      .FirstOrDefault();

    return Ok(new AgentDashboardMemoriesDto(
      MemoriesCount: memories.Count,
      LatestUpdatedAt: latestMemory?.UpdatedAt,
      LatestName: latestMemory?.Name));
  }
}

public record AgentDashboardBankrollDto(decimal TotalValue, decimal Balance, int DaysUntilPayday);

public record AgentDashboardBettingSummaryDto(
  int SettledSlipsCount,
  int SettledSelectionsCount,
  int WonSlipsCount,
  int LostSlipsCount,
  decimal WinRatePercent,
  decimal LossRatePercent);

public record AgentDashboardResearchBettingSummaryDto(
  int SettledSelectionsCount,
  int WonSelectionsCount,
  int LostSelectionsCount,
  decimal WinRatePercent,
  decimal LossRatePercent);

public record AgentDashboardBettingSummaryDetailsDto(
  int WonSlipsCount,
  int LostSlipsCount,
  int WonSelectionsCount,
  int LostSelectionsCount,
  IReadOnlyList<BetSlipListItemDto> Slips);

public record AgentDashboardPendingBetsDto(
  int PendingSlipsCount,
  decimal PendingStakeTotal,
  decimal PendingPotentialPayoutTotal,
  DateTime? LatestPendingCreatedAt);

public record AgentDashboardSessionsDto(
  int SessionsCount,
  DateTime? LatestStartedAt,
  string? LatestPhaseName);

public record AgentDashboardMemoriesDto(
  int MemoriesCount,
  DateTime? LatestUpdatedAt,
  string? LatestName);
