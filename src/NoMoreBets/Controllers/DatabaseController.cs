using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Infrastructure.Database;

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
