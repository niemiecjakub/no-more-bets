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
  public async Task<ActionResult<IReadOnlyList<MatchDto>>> GetMatches(
    [FromQuery] int? matchStatusId = null,
    [FromQuery] int[]? leagueIds = null,
    CancellationToken cancellationToken = default)
  {
    var selectedLeagueIds = (leagueIds ?? []).Distinct().ToArray();
    var hasLeagueFilter = selectedLeagueIds.Length > 0;

    var matchesQuery = db.Match.AsQueryable();
    if (matchStatusId.HasValue)
      matchesQuery = matchesQuery.Where(m => m.MatchStatusId == matchStatusId.Value);
    if (hasLeagueFilter)
      matchesQuery = matchesQuery.Where(m =>
        m.Stage != null &&
        selectedLeagueIds.Contains(m.Stage.Season.LeagueId));

    var readyForPrediction = await mediator
      .Send(new GetUpcomingMatchesReadyForPredictionQuery(ExcludeWithExistingResearch: false), cancellationToken)
      .ConfigureAwait(false);

    var completeSet = readyForPrediction.Select(m => m.Id).ToHashSet();

    var matchIdsWithLineup = await db.Lineup
      .Select(l => l.MatchId)
      .Distinct()
      .ToListAsync(cancellationToken);
    var hasLineupSet = matchIdsWithLineup.ToHashSet();

    var matchIdsWithOdds = await db.BettingOddsSnapshot
      .Select(b => b.MatchId)
      .Distinct()
      .ToListAsync(cancellationToken);
    var hasOddsSet = matchIdsWithOdds.ToHashSet();

    var matchIdsWithHeadToHead = await db.Match
      .Where(m => db.Head2Head.Any(h =>
        (h.Team1Id == m.HomeClubId && h.Team2Id == m.AwayClubId) ||
        (h.Team1Id == m.AwayClubId && h.Team2Id == m.HomeClubId)))
      .Select(m => m.Id)
      .Distinct()
      .ToListAsync(cancellationToken);
    var hasHeadToHeadSet = matchIdsWithHeadToHead.ToHashSet();

    var matchIdsWithResearch = await db.MatchAnalysis
      .Where(a => a.Code == MatchAnalysis.ResearchCode)
      .Select(a => a.MatchId)
      .Distinct()
      .ToListAsync(cancellationToken);
    var hasResearchSet = matchIdsWithResearch.ToHashSet();
    var matchIdsWithResearchBet = await db.BetSelection
      .Where(sel => sel.BetSlip.AgentSession != null && sel.BetSlip.AgentSession.Phase == AgentSessionPhase.Research)
      .Select(sel => sel.MatchId)
      .Distinct()
      .ToListAsync(cancellationToken);
    var hasResearchBetSet = matchIdsWithResearchBet.ToHashSet();

    var list = await matchesQuery
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .Include(m => m.MatchStatusEntity)
      .Include(m => m.Stage)
        .ThenInclude(s => s!.Season)
        .ThenInclude(se => se.League)
      .OrderByDescending(m => m.MatchDate)
      .ToListAsync(cancellationToken);

    var result = list
      .Select(m => new MatchDto(
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
        hasHeadToHeadSet.Contains(m.Id)))
      .ToList();

    return Ok(result);
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
}
