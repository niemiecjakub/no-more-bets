using MediatR;
using Microsoft.AspNetCore.Mvc;
using NoMoreBets.Application.AgentDashboard.GetAgentDashboardBankroll;
using NoMoreBets.Application.AgentDashboard.GetAgentDashboardBettingSummary;
using NoMoreBets.Application.AgentDashboard.GetAgentDashboardBettingSummaryDetails;
using NoMoreBets.Application.AgentDashboard.GetAgentDashboardBettingSummarySlips;
using NoMoreBets.Application.AgentDashboard.GetAgentDashboardMemories;
using NoMoreBets.Application.AgentDashboard.GetAgentDashboardPendingBets;
using NoMoreBets.Application.AgentDashboard.GetAgentDashboardResearchBettingSummary;
using NoMoreBets.Application.AgentDashboard.GetAgentDashboardSessions;
using NoMoreBets.Application.Betting.Common;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api/agent/dashboard")]
public class AgentDashboardController(IMediator mediator) : ControllerBase
{
  [HttpGet("bankroll")]
  public async Task<ActionResult<AgentDashboardBankrollDto>> GetBankrollWidget(
    [FromQuery] string[]? seasonYears,
    CancellationToken cancellationToken = default)
  {
    var result = await mediator
      .Send(new GetAgentDashboardBankrollQuery(SeasonYearQueryExtensions.Normalize(seasonYears)), cancellationToken)
      .ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("betting-summary")]
  public async Task<ActionResult<AgentDashboardBettingSummaryDto>> GetBettingSummaryWidget(
    [FromQuery] string[]? seasonYears,
    CancellationToken cancellationToken = default)
  {
    var result = await mediator
      .Send(new GetAgentDashboardBettingSummaryQuery(SeasonYearQueryExtensions.Normalize(seasonYears)), cancellationToken)
      .ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("research-betting-summary")]
  public async Task<ActionResult<AgentDashboardResearchBettingSummaryDto>> GetResearchBettingSummaryWidget(
    [FromQuery] int[]? leagueIds,
    [FromQuery(Name = "leagueIds[]")] int[]? leagueIdsBracket,
    [FromQuery] string[]? seasonYears,
    CancellationToken cancellationToken = default)
  {
    var selectedLeagueIds = (leagueIds ?? [])
      .Concat(leagueIdsBracket ?? [])
      .Where(id => id > 0)
      .Distinct()
      .ToArray();

    var result = await mediator
      .Send(
        new GetAgentDashboardResearchBettingSummaryQuery(
          selectedLeagueIds,
          SeasonYearQueryExtensions.Normalize(seasonYears)),
        cancellationToken)
      .ConfigureAwait(false);

    return Ok(result);
  }

  [HttpGet("betting-summary/details")]
  public async Task<ActionResult<AgentDashboardBettingSummaryDetailsDto>> GetBettingSummaryDetails(
    [FromQuery] string[]? seasonYears,
    CancellationToken cancellationToken = default)
  {
    var result = await mediator
      .Send(
        new GetAgentDashboardBettingSummaryDetailsQuery(SeasonYearQueryExtensions.Normalize(seasonYears)),
        cancellationToken)
      .ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("betting-summary/slips")]
  public async Task<ActionResult<Paged<BetSlipListItemDto>>> GetBettingSummarySlips(
    [FromQuery] int limit = 10,
    [FromQuery] DateTime? afterCreatedAt = null,
    [FromQuery] int? afterId = null,
    [FromQuery] string[]? seasonYears = null,
    CancellationToken cancellationToken = default)
  {
    limit = Math.Clamp(limit, 1, 100);

    if (afterCreatedAt is null != afterId is null)
    {
      return BadRequest("afterCreatedAt and afterId must both be provided or omitted.");
    }

    DateTime? afterCreatedAtUtc = afterCreatedAt is not null
      ? UtcDateTime.ToUtc(afterCreatedAt.Value)
      : null;

    var result = await mediator.Send(
      new GetAgentDashboardBettingSummarySlipsQuery(
        limit,
        afterCreatedAtUtc,
        afterId,
        SeasonYearQueryExtensions.Normalize(seasonYears)),
      cancellationToken).ConfigureAwait(false);

    return Ok(result);
  }

  [HttpGet("pending-bets")]
  public async Task<ActionResult<AgentDashboardPendingBetsDto>> GetPendingBetsWidget(
    [FromQuery] string[]? seasonYears,
    CancellationToken cancellationToken = default)
  {
    var result = await mediator
      .Send(new GetAgentDashboardPendingBetsQuery(SeasonYearQueryExtensions.Normalize(seasonYears)), cancellationToken)
      .ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("sessions")]
  public async Task<ActionResult<AgentDashboardSessionsDto>> GetSessionsWidget(
    [FromQuery] string[]? seasonYears,
    CancellationToken cancellationToken = default)
  {
    var result = await mediator
      .Send(new GetAgentDashboardSessionsQuery(SeasonYearQueryExtensions.Normalize(seasonYears)), cancellationToken)
      .ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("memories")]
  public async Task<ActionResult<AgentDashboardMemoriesDto>> GetMemoriesWidget(
    CancellationToken cancellationToken = default)
  {
    var result = await mediator.Send(new GetAgentDashboardMemoriesQuery(), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }
}
