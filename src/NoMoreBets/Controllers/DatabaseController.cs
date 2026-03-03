using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
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
      .Select(l => new LeagueDto(l.Id, l.Name, l.SoccerdataId))
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
      .Select(c => new ClubDto(c.Id, c.Name, c.LeagueId, c.SoccerdataId, c.League.Name))
      .ToListAsync(cancellationToken);
    return Ok(list);
  }

  /// <summary>
  /// Gets matches from the database.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List of matches with home/away club names and status.</returns>
  [HttpGet("matches")]
  public async Task<ActionResult<IReadOnlyList<MatchDto>>> GetMatches(CancellationToken cancellationToken = default)
  {
    var list = await db.Match
      .Include(m => m.HomeClub)
      .Include(m => m.AwayClub)
      .Include(m => m.MatchStatusEntity)
      .OrderBy(m => m.MatchDate)
      .Select(m => new MatchDto(
        m.Id,
        m.SoccerdataId,
        m.MatchDate,
        m.HomeClubId,
        m.AwayClubId,
        m.HomeClub.Name,
        m.AwayClub.Name,
        m.MatchStatusId,
        m.MatchStatusEntity!.Name,
        m.HomeGoals,
        m.AwayGoals,
        m.BetclicUrl))
      .ToListAsync(cancellationToken);
    return Ok(list);
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
      .Select(m => new MatchDto(
        m.Id,
        m.SoccerdataId,
        m.MatchDate,
        m.HomeClubId,
        m.AwayClubId,
        m.HomeClub.Name,
        m.AwayClub.Name,
        m.MatchStatusId,
        m.MatchStatusEntity!.Name,
        m.HomeGoals,
        m.AwayGoals,
        m.BetclicUrl))
      .ToListAsync(cancellationToken);
    return Ok(list);
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
  /// <returns>List of odds snapshots for that event type, newest first; empty list if none.</returns>
  [HttpGet("matches/{matchId:int}/betting-odds-history")]
  public async Task<ActionResult<IReadOnlyList<BettingOddsEventHistoryDto>>> GetMatchBettingOddsHistory(
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

    var list = new List<BettingOddsEventHistoryDto>();
    foreach (var snapshot in snapshots)
    {
      var row = snapshot.Rows.FirstOrDefault(r => r.EventTypeId == eventTypeId);
      if (row == null)
        continue;
      var eventJson = JsonSerializer.Deserialize<JsonElement>(row.EventJson);
      list.Add(new BettingOddsEventHistoryDto(
        snapshot.Id,
        snapshot.SnapshotTime,
        row.EventTypeId,
        row.EventTypeEntity.Name,
        eventJson));
    }

    return Ok(list);
  }
}

public record LeagueDto(int Id, string Name, int SoccerdataId);

public record ClubDto(int Id, string Name, int LeagueId, int SoccerdataId, string LeagueName);

public record MatchDto(
  int Id,
  int? SoccerdataId,
  DateTime MatchDate,
  int HomeClubId,
  int AwayClubId,
  string HomeClubName,
  string AwayClubName,
  int MatchStatusId,
  string MatchStatusName,
  int? HomeGoals,
  int? AwayGoals,
  string? BetclicUrl);

public record LeagueTableDto(
  long SnapshotId,
  int LeagueId,
  int SeasonId,
  DateOnly SnapshotDate,
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

public record BettingOddsEventHistoryDto(
  long SnapshotId,
  DateTime SnapshotTime,
  int EventTypeId,
  string EventTypeName,
  JsonElement EventJson);
