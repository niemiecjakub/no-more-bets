using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Features.Fotmob.RefreshLeagueTableSnapshot.Dtos;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.Fotmob.RefreshLeagueTableSnapshot;

/// <summary>
/// Handles <see cref="GetLeagueTableSnapshotQuery"/> by loading the snapshot (and rows with club names) by season and optional date.
/// </summary>
public class GetLeagueTableSnapshotHandler(AppDbContext db)
  : IRequestHandler<GetLeagueTableSnapshotQuery, LeagueTableSnapshotDto?>
{
  /// <inheritdoc />
  public async Task<LeagueTableSnapshotDto?> Handle(GetLeagueTableSnapshotQuery request, CancellationToken cancellationToken)
  {
    var seasonId = request.SeasonId ?? await db.Season
      .Where(s => s.LeagueId == request.LeagueId)
      .MaxAsync(s => (int?)s.Id, cancellationToken)
      .ConfigureAwait(false);

    if (seasonId == null)
    {
      return null;
    }

    var query = db.LeagueTableSnapshot
      .Where(s => s.SeasonId == seasonId && s.LeagueId == request.LeagueId);

    if (request.SnapshotDate.HasValue)
    {
      query = query.Where(s => s.SnapshotDate == request.SnapshotDate.Value);
    }
    else
    {
      var maxDate = await db.LeagueTableSnapshot
        .Where(s => s.SeasonId == seasonId)
        .MaxAsync(s => (DateOnly?)s.SnapshotDate, cancellationToken)
        .ConfigureAwait(false);
      if (maxDate == null)
      {
        return null;
      }
      query = query.Where(s => s.SnapshotDate == maxDate.Value);
    }

    var snapshot = await query
      .Include(s => s.Rows)
      .ThenInclude(r => r.Club)
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);

    if (snapshot == null)
    {
      return null;
    }

    var rows = snapshot.Rows
      .OrderBy(r => r.Position)
      .Select(r => new LeagueTableSnapshotRowDto(
        r.ClubId,
        r.Club.Name,
        r.Position,
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

    return new LeagueTableSnapshotDto(
      snapshot.Id,
      snapshot.LeagueId,
      snapshot.SeasonId,
      snapshot.SnapshotDate,
      snapshot.CreatedAt,
      rows);
  }
}
