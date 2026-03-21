using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Application.Common.Dto.Betting;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Controllers;

/// <summary>
/// Endpoints to fetch matches, clubs, and leagues from the database.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DatabaseController(AppDbContext db) : ControllerBase
{
  /// <summary>
  /// Gets leagues from the database.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of leagues.</returns>
  [HttpGet("leagues")]
  public async Task<ActionResult<IReadOnlyList<LeagueDto>>> GetLeagues(CancellationToken cancellationToken = default)
  {
    var list = await db.League
      .OrderBy(l => l.Name)
      .Select(l => new LeagueDto(l.Id, l.Name, l.Slug))
      .ToListAsync(cancellationToken);
    return Ok(list);
  }

  /// <summary>
  /// Gets clubs from the database.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of clubs.</returns>
  [HttpGet("clubs")]
  public async Task<ActionResult<IReadOnlyList<ClubDto>>> GetClubs(CancellationToken cancellationToken = default)
  {
    var list = await db.Club
      .Include(c => c.League)
      .OrderBy(c => c.Name)
      .Select(c => new ClubDto(c.Id, c.Name, c.LeagueId, c.League.Name, c.Slug, c.League.Slug))
      .ToListAsync(cancellationToken);
    return Ok(list);
  }

  /// <summary>
  /// Gets the latest daily summary for the specified club.
  /// </summary>
  /// <param name="clubId">Club ID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Latest daily summary or 404 if none exists.</returns>
  [HttpGet("clubs/{clubId:int}/daily-summary")]
  public async Task<ActionResult<ClubDailySummaryDto>> GetClubDailySummary(
    int clubId,
    CancellationToken cancellationToken = default)
  {
    var summary = await db.ClubDailySummary
      .Where(s => s.ClubId == clubId)
      .Include(s => s.Club)
      .OrderByDescending(s => s.Date)
      .FirstOrDefaultAsync(cancellationToken);

    if (summary == null)
      return NotFound();

    return Ok(new ClubDailySummaryDto(summary.Id, summary.Club.Name, summary.Date, summary.Summary));
  }

  /// <summary>
  /// Gets all bet slips from the database, newest first.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of bet slips with selections and match info.</returns>
  [HttpGet("bet-slips")]
  public async Task<ActionResult<IReadOnlyList<BetSlipListItemDto>>> GetBetSlips(CancellationToken cancellationToken = default)
  {
    var slips = await db.BetSlip
      .Include(s => s.BetStatusEntity)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.HomeClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.Match)
          .ThenInclude(m => m!.AwayClub)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.EventTypeEntity)
      .Include(s => s.Selections)
        .ThenInclude(sel => sel.BetStatusEntity)
      .OrderByDescending(s => s.CreatedAt)
      .ToListAsync(cancellationToken);

    var result = slips
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
            sel.EventTypeEntity.Name,
            sel.OutcomeKey,
            sel.OddsAtPlacement,
            sel.StatusId,
            sel.BetStatusEntity.Name))
          .ToList()))
      .ToList();

    return Ok(result);
  }

  /// <summary>
  /// Gets matches from the database.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of matches with home/away club names and status.</returns>
  [HttpGet("matches")]
  public async Task<ActionResult<IReadOnlyList<MatchDto>>> GetMatches(CancellationToken cancellationToken = default)
  {
    var completeMatchIds = await db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming)
      .Where(m => db.MatchPreview.Any(mp => mp.MatchId == m.Id))
      .Where(m => db.Lineup.Any(l => l.MatchId == m.Id))
      .Where(m => db.BettingOddsSnapshot.Any(b => b.MatchId == m.Id))
      .Where(m => db.Head2Head.Any(h =>
        (h.Team1Id == m.HomeClubId && h.Team2Id == m.AwayClubId) ||
        (h.Team1Id == m.AwayClubId && h.Team2Id == m.HomeClubId)))
      .Select(m => m.Id)
      .ToListAsync(cancellationToken);

    var completeSet = completeMatchIds.ToHashSet();

    var matchIdsWithAnalysis = await db.MatchAnalysis
      .Select(a => a.MatchId)
      .Distinct()
      .ToListAsync(cancellationToken);
    var hasAnalysisSet = matchIdsWithAnalysis.ToHashSet();

    var list = await db.Match
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .Include(m => m.MatchStatusEntity)
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
        m.MatchStatusId,
        m.MatchStatusEntity!.Name,
        m.HomeGoals,
        m.AwayGoals,
        m.BetclicUrl,
        completeSet.Contains(m.Id),
        hasAnalysisSet.Contains(m.Id)))
      .ToList();

    return Ok(result);
  }

  /// <summary>
  /// Gets upcoming matches from the database.
  /// </summary>
  /// <param name="leagueId">Optional league ID to filter by.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of upcoming matches.</returns>
  [HttpGet("upcoming-games")]
  public async Task<ActionResult<IReadOnlyList<MatchDto>>> GetUpcomingGames(
    [FromQuery] int? leagueId,
    CancellationToken cancellationToken = default)
  {
    var query = db.Match
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .Include(m => m.MatchStatusEntity)
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming);

    if (leagueId.HasValue)
      query = query.Where(m => m.HomeClub.LeagueId == leagueId.Value);

    var list = await query
      .OrderBy(m => m.MatchDate)
      .ToListAsync(cancellationToken);

    var completeMatchIds = await db.Match
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming)
      .Where(m => db.MatchPreview.Any(mp => mp.MatchId == m.Id))
      .Where(m => db.Lineup.Any(l => l.MatchId == m.Id))
      .Where(m => db.BettingOddsSnapshot.Any(b => b.MatchId == m.Id))
      .Where(m => db.Head2Head.Any(h =>
        (h.Team1Id == m.HomeClubId && h.Team2Id == m.AwayClubId) ||
        (h.Team1Id == m.AwayClubId && h.Team2Id == m.HomeClubId)))
      .Select(m => m.Id)
      .ToListAsync(cancellationToken);
    var completeSet = completeMatchIds.ToHashSet();

    var matchIdsWithAnalysis = await db.MatchAnalysis
      .Select(a => a.MatchId)
      .Distinct()
      .ToListAsync(cancellationToken);
    var hasAnalysisSet = matchIdsWithAnalysis.ToHashSet();

    var result = list
      .Select(m => new MatchDto(
        m.Id,
        m.MatchDate,
        m.HomeClubId,
        m.AwayClubId,
        m.HomeClub.Name,
        m.AwayClub.Name,
        m.MatchStatusId,
        m.MatchStatusEntity!.Name,
        m.HomeGoals,
        m.AwayGoals,
        m.BetclicUrl,
        completeSet.Contains(m.Id),
        hasAnalysisSet.Contains(m.Id)))
      .ToList();

    return Ok(result);
  }

  /// <summary>
  /// Gets matches that have complete data: MatchPreview, Head2Head, Lineup, and at least one BettingOddsSnapshot.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of matches with home/away club names and status.</returns>
  [HttpGet("matches/complete")]
  public async Task<ActionResult<IReadOnlyList<MatchDto>>> GetMatchesWithCompleteData(
    CancellationToken cancellationToken = default)
  {
    var matchIdsWithAnalysis = await db.MatchAnalysis
      .Select(a => a.MatchId)
      .Distinct()
      .ToListAsync(cancellationToken);
    var hasAnalysisSet = matchIdsWithAnalysis.ToHashSet();

    var list = await db.Match
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .Include(m => m.MatchStatusEntity)
      .Where(m => m.MatchStatusId == (int)MatchStatus.Upcomming)
      .Where(m => db.MatchPreview.Any(mp => mp.MatchId == m.Id))
      .Where(m => db.Lineup.Any(l => l.MatchId == m.Id))
      .Where(m => db.BettingOddsSnapshot.Any(b => b.MatchId == m.Id))
      .Where(m => db.Head2Head.Any(h =>
        (h.Team1Id == m.HomeClubId && h.Team2Id == m.AwayClubId) ||
        (h.Team1Id == m.AwayClubId && h.Team2Id == m.HomeClubId)))
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
        m.MatchStatusId,
        m.MatchStatusEntity!.Name,
        m.HomeGoals,
        m.AwayGoals,
        m.BetclicUrl,
        true,
        hasAnalysisSet.Contains(m.Id)))
      .ToList();
    return Ok(result);
  }

  /// <summary>
  /// Gets matches that share the same game URL (BetclicUrl). Returns only URLs that have more than one match.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of game URL and the matches that use it.</returns>
  [HttpGet("matches/duplicated-by-game-url")]
  public async Task<ActionResult<IReadOnlyList<DuplicatedMatchesByGameUrlDto>>> GetDuplicatedMatchesByGameUrl(
    CancellationToken cancellationToken = default)
  {
    var duplicatedUrls = await db.Match
      .Where(m => m.BetclicUrl != null)
      .GroupBy(m => m.BetclicUrl)
      .Where(g => g.Count() > 1)
      .Select(g => g.Key!)
      .ToListAsync(cancellationToken);

    if (duplicatedUrls.Count == 0)
      return Ok((IReadOnlyList<DuplicatedMatchesByGameUrlDto>)Array.Empty<DuplicatedMatchesByGameUrlDto>());

    var matches = await db.Match
      .Where(m => m.BetclicUrl != null && duplicatedUrls.Contains(m.BetclicUrl))
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .Include(m => m.MatchStatusEntity)
      .OrderBy(m => m.BetclicUrl)
      .ThenBy(m => m.MatchDate)
      .ToListAsync(cancellationToken);

    var matchIdsWithAnalysis = await db.MatchAnalysis
      .Select(a => a.MatchId)
      .Distinct()
      .ToListAsync(cancellationToken);
    var hasAnalysisSet = matchIdsWithAnalysis.ToHashSet();

    var result = matches
      .GroupBy(m => m.BetclicUrl!)
      .OrderBy(g => g.Key)
      .Select(g => new DuplicatedMatchesByGameUrlDto(
        g.Key,
        g.Select(m => new MatchDto(
          m.Id,
          m.MatchDate,
          m.HomeClubId,
          m.AwayClubId,
          m.HomeClub.Name,
          m.AwayClub.Name,
          m.MatchStatusId,
          m.MatchStatusEntity!.Name,
          m.HomeGoals,
          m.AwayGoals,
          m.BetclicUrl,
          false,
          hasAnalysisSet.Contains(m.Id))).ToList()))
      .ToList();

    return Ok(result);
  }

  /// <summary>
  /// Gets the latest league table for the specified league.
  /// </summary>
  /// <param name="leagueId">League ID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Latest league table snapshot or 404 if none exists.</returns>
  [HttpGet("leagues/{leagueId:int}/table")]
  public async Task<ActionResult<LeagueTableDto>> GetLeagueTable(
    int leagueId,
    CancellationToken cancellationToken = default)
  {
    var snapshot = await db.LeagueTableSnapshot
      .Where(s => s.LeagueId == leagueId)
      .Include(s => s.League)
      .Include(s => s.Rows)
      .ThenInclude(r => r.Club)
      .OrderByDescending(s => s.SnapshotDate)
      .FirstOrDefaultAsync(cancellationToken);

    if (snapshot == null)
      return NotFound();

    var rows = snapshot.Rows
      .OrderBy(r => r.Position)
      .Select(r => new LeagueTableRowDto(
        r.Position,
        r.ClubId,
        r.Club.Name,
        r.MatchesPlayed,
        r.Wins,
        r.Draws,
        r.Losses,
        r.GoalsFor,
        r.GoalsAgainst,
        r.GoalDifference,
        r.Points,
        r.Xg,
        r.XgDiff,
        r.Xga,
        r.XgaDiff,
        r.Xpts,
        r.XptsDiff))
      .ToList();

    return Ok(new LeagueTableDto(
      snapshot.Id,
      snapshot.LeagueId,
      snapshot.SeasonId,
      snapshot.SnapshotDate,
      snapshot.League.Name,
      rows));
  }

  /// <summary>
  /// Gets the lineup for the specified match.
  /// </summary>
  /// <param name="matchId">Match ID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Match lineup or 404 if not found.</returns>
  [HttpGet("matches/{matchId:int}/lineup")]
  public async Task<ActionResult<MatchLineupDto>> GetMatchLineup(
    int matchId,
    CancellationToken cancellationToken = default)
  {
    var lineup = await db.Lineup
      .Include(l => l.Match)
      .ThenInclude(m => m!.HomeClub)
      .Include(l => l.Match)
      .ThenInclude(m => m!.AwayClub)
      .FirstOrDefaultAsync(l => l.MatchId == matchId, cancellationToken);

    if (lineup == null)
      return NotFound();

    var match = lineup.Match;
    var dto = new MatchLineupDto(
      lineup.MatchId,
      match.HomeClub.Name,
      match.AwayClub.Name,
      lineup.UpdatedAt,
      lineup.GetHomeTeamLineup(),
      lineup.GetAwayTeamLineup());
    return Ok(dto);
  }

  /// <summary>
  /// Gets the betting odds history for a specific event type on a match, newest first.
  /// </summary>
  /// <param name="matchId">Match ID.</param>
  /// <param name="eventTypeId">Betting event type ID (e.g. 5 = MatchResult, 1 = OverUnderGoals).</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Aggregated odds history with eventTypeId, eventTypeName, and history (title + options with odds time series).</returns>
  [HttpGet("matches/{matchId:int}/betting-odds-history")]
  public async Task<ActionResult<BettingOddsHistoryResponseDto>> GetMatchBettingOddsHistory(
    int matchId,
    [FromQuery] int eventTypeId,
    CancellationToken cancellationToken = default)
  {
    var snapshots = await db.BettingOddsSnapshot
      .Where(s => s.MatchId == matchId)
      .Include(s => s.Rows)
      .ThenInclude(r => r.EventTypeEntity)
      .OrderByDescending(s => s.SnapshotTime)
      .ToListAsync(cancellationToken);

    string? eventTypeName = null;
    string? historyTitle = null;
    List<string>? optionOrder = null;
    var oddsByLabel = new Dictionary<string, List<OddsPointDto>>(StringComparer.Ordinal);

    foreach (var snapshot in snapshots)
    {
      var row = snapshot.Rows.FirstOrDefault(r => r.EventTypeId == eventTypeId);
      if (row == null)
        continue;

      eventTypeName ??= row.EventTypeEntity.Name;

      BookmakerEvent? ev;
      try
      {
        ev = JsonSerializer.Deserialize<BookmakerEvent>(row.EventJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
      }
      catch
      {
        continue;
      }

      if (ev == null)
        continue;

      historyTitle ??= ev.Title;
      optionOrder ??= ev.Options.Select(o => o.Label).ToList();

      foreach (var opt in ev.Options)
      {
        if (!oddsByLabel.TryGetValue(opt.Label, out var list))
        {
          list = new List<OddsPointDto>();
          oddsByLabel[opt.Label] = list;
        }
        list.Add(new OddsPointDto(opt.Odds, snapshot.SnapshotTime));
      }
    }

    var historyOptions = (optionOrder ?? (IReadOnlyList<string>)Array.Empty<string>())
      .Select(label => new BettingOddsHistoryOptionDto(
        label,
        oddsByLabel.TryGetValue(label, out var odds) ? odds : (IReadOnlyList<OddsPointDto>)Array.Empty<OddsPointDto>()))
      .ToList();

    var response = new BettingOddsHistoryResponseDto(
      eventTypeId,
      eventTypeName ?? "",
      new BettingOddsHistorySectionDto(historyTitle, historyOptions));

    return Ok(response);
  }

  /// <summary>
  /// Gets match header and all analyses for the specified match.
  /// </summary>
  /// <param name="matchId">Match ID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Match header and analyses, or 404 if match not found.</returns>
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

    var analysisEntities = await db.MatchAnalysis
      .Where(a => a.MatchId == matchId)
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
      match.MatchDate,
      analyses);
    return Ok(page);
  }

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

public record LeagueDto(int Id, string Name, string Slug);

public record ClubDto(
  int Id,
  string Name,
  int LeagueId,
  string LeagueName,
  string Slug,
  string LeagueSlug);

public record ClubDailySummaryDto(int Id, string ClubName, DateOnly Date, string Summary);

public record MatchDto(
  int Id,
  DateTime MatchDate,
  int HomeClubId,
  int AwayClubId,
  string HomeClubName,
  string AwayClubName,
  int MatchStatusId,
  string MatchStatusName,
  int? HomeGoals,
  int? AwayGoals,
  string? BetclicUrl,
  bool IsReadyToPredict = false,
  bool HasAnalysis = false);

public record LeagueTableDto(
  long SnapshotId,
  int LeagueId,
  int SeasonId,
  DateOnly SnapshotDate,
  string LeagueName,
  IReadOnlyList<LeagueTableRowDto> Rows);

public record LeagueTableRowDto(
  int Position,
  int ClubId,
  string ClubName,
  int MatchesPlayed,
  int Wins,
  int Draws,
  int Losses,
  int GoalsFor,
  int GoalsAgainst,
  int GoalDifference,
  int Points,
  decimal Xg,
  decimal XgDiff,
  decimal Xga,
  decimal XgaDiff,
  decimal Xpts,
  decimal XptsDiff);

public record MatchLineupDto(
  int MatchId,
  string HomeClubName,
  string AwayClubName,
  DateTime UpdatedAt,
  TeamLineup HomeTeam,
  TeamLineup AwayTeam);

public record OddsPointDto(double Value, DateTime Date);

public record BettingOddsHistoryOptionDto(string Title, IReadOnlyList<OddsPointDto> Odds);

public record BettingOddsHistorySectionDto(string? Title, IReadOnlyList<BettingOddsHistoryOptionDto> Options);

public record BettingOddsHistoryResponseDto(
  int EventTypeId,
  string EventTypeName,
  BettingOddsHistorySectionDto History);

public record DuplicatedMatchesByGameUrlDto(string GameUrl, IReadOnlyList<MatchDto> Matches);

public record StructuredMatchAnalysisDto(
  string? Context,
  string? Form,
  string? Tactics,
  string? Squad,
  string? Statistics,
  string? Market,
  string? MatchProjection,
  string? Prediction);

public record MatchAnalysisItemDto(
  int Id,
  string Code,
  string Content,
  StructuredMatchAnalysisDto? Structured);

public record MatchAnalysisPageDto(
  int MatchId,
  string HomeClubName,
  string AwayClubName,
  DateTime MatchDate,
  IReadOnlyList<MatchAnalysisItemDto> Analyses);

public record BetSelectionItemDto(
  int MatchId,
  string HomeClubName,
  string AwayClubName,
  string EventTypeName,
  string OutcomeKey,
  decimal OddsAtPlacement,
  int StatusId,
  string StatusName);

public record BetSlipListItemDto(
  int Id,
  DateTime CreatedAt,
  decimal StakeAmount,
  decimal TotalOdds,
  decimal PotentialPayout,
  int StatusId,
  string StatusName,
  IReadOnlyList<BetSelectionItemDto> Selections);
