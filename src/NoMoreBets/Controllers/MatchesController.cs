using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Application.Matches.GetHeadToHeadStats;
using NoMoreBets.Application.Matches.GetMatchInjuries;
using NoMoreBets.Application.Matches.GetMatchLineups;
using NoMoreBets.Application.Matches.GetMatchesReadyForPrediction;
using NoMoreBets.Controllers.Models;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Controllers;

[ApiController]
[Route("api")]
public class MatchesController(AppDbContext db, IMediator mediator) : ControllerBase
{
  [HttpGet("matches")]
  public async Task<ActionResult<MatchesPageDto>> GetMatches(
    [FromQuery] int? matchStatusId = null,
    [FromQuery] int[]? leagueIds = null,
    [FromQuery] int limit = 25,
    [FromQuery] DateTime? afterMatchDate = null,
    [FromQuery] int? afterId = null,
    CancellationToken cancellationToken = default)
  {
    limit = Math.Clamp(limit, 1, 100);

    if (afterMatchDate is null != afterId is null)
      return BadRequest("afterMatchDate and afterId must both be provided or omitted.");

    var selectedLeagueIds = (leagueIds ?? []).Distinct().ToArray();
    var hasLeagueFilter = selectedLeagueIds.Length > 0;

    var matchesQuery = db.Match.AsNoTracking().AsQueryable();
    if (matchStatusId.HasValue)
      matchesQuery = matchesQuery.Where(m => m.MatchStatusId == matchStatusId.Value);
    if (hasLeagueFilter)
      matchesQuery = matchesQuery.Where(m =>
        m.Stage != null &&
        selectedLeagueIds.Contains(m.Stage.Season.LeagueId));

    if (afterMatchDate is not null && afterId is not null)
    {
      var cursorMatchDate = DateTimeQueryExtensions.ToUtc(afterMatchDate.Value);
      var cursorId = afterId.Value;
      matchesQuery = matchesQuery.Where(m =>
        m.MatchDate < cursorMatchDate
        || (m.MatchDate == cursorMatchDate && m.Id < cursorId));
    }

    var rows = await matchesQuery
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .Include(m => m.MatchStatusEntity)
      .Include(m => m.Stage)
        .ThenInclude(s => s!.Season)
        .ThenInclude(se => se.League)
      .OrderByDescending(m => m.MatchDate)
      .ThenByDescending(m => m.Id)
      .Take(limit + 1)
      .ToListAsync(cancellationToken);

    var hasMore = rows.Count > limit;
    if (hasMore)
      rows.RemoveAt(rows.Count - 1);

    var pageIds = rows.Select(m => m.Id).ToList();

    var readyForPrediction = await mediator
      .Send(new GetUpcomingMatchesReadyForPredictionQuery(ExcludeWithExistingResearch: false), cancellationToken)
      .ConfigureAwait(false);
    var completeSet = readyForPrediction.Select(m => m.Id).ToHashSet();

    HashSet<int> hasLineupSet = [];
    HashSet<int> hasOddsSet = [];
    HashSet<int> hasHeadToHeadSet = [];
    HashSet<int> hasResearchSet = [];
    HashSet<int> hasResearchBetSet = [];

    if (pageIds.Count > 0)
    {
      hasLineupSet = (await db.Lineup
        .Where(l => pageIds.Contains(l.MatchId))
        .Select(l => l.MatchId)
        .Distinct()
        .ToListAsync(cancellationToken))
        .ToHashSet();

      hasOddsSet = (await db.BettingOddsSnapshot
        .Where(b => pageIds.Contains(b.MatchId))
        .Select(b => b.MatchId)
        .Distinct()
        .ToListAsync(cancellationToken))
        .ToHashSet();

      hasHeadToHeadSet = (await db.Match
        .Where(m => pageIds.Contains(m.Id) && db.Head2Head.Any(h =>
          (h.Team1Id == m.HomeClubId && h.Team2Id == m.AwayClubId) ||
          (h.Team1Id == m.AwayClubId && h.Team2Id == m.HomeClubId)))
        .Select(m => m.Id)
        .Distinct()
        .ToListAsync(cancellationToken))
        .ToHashSet();

      hasResearchSet = (await db.MatchAnalysis
        .Where(a => pageIds.Contains(a.MatchId) && a.Code == MatchAnalysis.ResearchCode)
        .Select(a => a.MatchId)
        .Distinct()
        .ToListAsync(cancellationToken))
        .ToHashSet();

      hasResearchBetSet = (await db.BetSelection
        .Where(sel =>
          pageIds.Contains(sel.MatchId)
          && sel.BetSlip.AgentSession != null
          && sel.BetSlip.AgentSession.Phase == AgentSessionPhase.Research)
        .Select(sel => sel.MatchId)
        .Distinct()
        .ToListAsync(cancellationToken))
        .ToHashSet();
    }

    var items = rows
      .Select(m => MapToMatchDto(
        m,
        completeSet,
        hasResearchSet,
        hasResearchBetSet,
        hasLineupSet,
        hasOddsSet,
        hasHeadToHeadSet))
      .ToList();

    DateTime? nextCursorMatchDate = null;
    int? nextCursorId = null;
    if (hasMore && rows.Count > 0)
    {
      var lastItem = rows[^1];
      nextCursorMatchDate = lastItem.MatchDate;
      nextCursorId = lastItem.Id;
    }

    return Ok(new MatchesPageDto(items, hasMore, nextCursorMatchDate, nextCursorId));
  }

  [HttpGet("matches/{matchId:int}/analyses")]
  public async Task<ActionResult<MatchAnalysisPageDto>> GetMatchAnalyses(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    var match = await db.Match
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);

    if (match == null)
      return NotFound();

    var researchAgentSessionId = await db.MatchAnalysis
      .AsNoTracking()
      .Where(a => a.MatchId == matchId && a.Code == MatchAnalysis.ResearchCode)
      .OrderByDescending(a => a.Id)
      .Select(a => a.AgentSessionId)
      .FirstOrDefaultAsync(cancellationToken);

    var analysisEntities = await db.MatchAnalysis
      .Where(a => a.MatchId == matchId)
      .Where(a => a.Code != MatchAnalysis.ResearchCode)
      .OrderBy(a => a.Id)
      .ToListAsync(cancellationToken);

    var analyses = analysisEntities
      .Select(a => new MatchAnalysisItemDto(
        a.Id,
        a.Code,
        a.Content,
        MapStructured(a.GetAnalysis())))
      .ToList();

    var page = new MatchAnalysisPageDto(
      match.Id,
      match.HomeClub.Name,
      match.AwayClub.Name,
      match.HomeClub.Slug,
      match.AwayClub.Slug,
      match.MatchStatusId,
      match.HomeGoals,
      match.AwayGoals,
      match.MatchDate,
      analyses,
      researchAgentSessionId);
    return Ok(page);
  }

  [HttpGet("matchinsights/matches/{matchId:int}/lineups")]
  public async Task<ActionResult<MatchLineupResult?>> GetLineups(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetMatchLineupsQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matchinsights/matches/{matchId:int}/injuries")]
  public async Task<ActionResult<MatchInjuriesResult?>> GetInjuries(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetMatchInjuriesQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  [HttpGet("matchinsights/matches/{matchId:int}/head-to-head")]
  public async Task<ActionResult<H2H?>> GetHeadToHead(int matchId, CancellationToken cancellationToken = default)
  {
    if (!await MatchExists(matchId, cancellationToken).ConfigureAwait(false))
      return NotFound();

    var result = await mediator.Send(new GetHeadToHeadStatsQuery(matchId), cancellationToken).ConfigureAwait(false);
    return Ok(result);
  }

  private Task<bool> MatchExists(int matchId, CancellationToken cancellationToken) =>
    db.Match.AnyAsync(m => m.Id == matchId, cancellationToken);

  private static StructuredMatchAnalysisDto? MapStructured(StructuredMatchAnalysis? analysis) =>
    analysis == null
      ? null
      : new StructuredMatchAnalysisDto(
        analysis.Context,
        analysis.Form,
        analysis.Tactics,
        analysis.Squad,
        analysis.Statistics,
        analysis.Market,
        analysis.MatchProjection,
        analysis.Prediction);

  private static MatchDto MapToMatchDto(
    Match m,
    HashSet<int> completeSet,
    HashSet<int> hasResearchSet,
    HashSet<int> hasResearchBetSet,
    HashSet<int> hasLineupSet,
    HashSet<int> hasOddsSet,
    HashSet<int> hasHeadToHeadSet) =>
    new(
      m.Id,
      m.MatchDate,
      m.HomeClubId,
      m.AwayClubId,
      m.HomeClub.Name,
      m.AwayClub.Name,
      m.HomeClub.Slug,
      m.AwayClub.Slug,
      m.Stage == null ? string.Empty : m.Stage.Season.League.Name,
      m.Stage == null ? string.Empty : m.Stage.Season.League.Slug,
      m.MatchStatusId,
      m.MatchStatusEntity.Name,
      m.HomeGoals,
      m.AwayGoals,
      m.BetclicUrl,
      completeSet.Contains(m.Id),
      hasResearchSet.Contains(m.Id),
      hasResearchBetSet.Contains(m.Id),
      hasLineupSet.Contains(m.Id),
      hasOddsSet.Contains(m.Id),
      hasHeadToHeadSet.Contains(m.Id));
}

public record MatchesPageDto(
  IReadOnlyList<MatchDto> Items,
  bool HasMore,
  DateTime? NextCursorMatchDate,
  int? NextCursorId);
