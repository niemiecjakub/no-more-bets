using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Entity;
using NoMoreBets.Features.Fotmob.GetFotmobXgStats.Dtos;
using NoMoreBets.Features.Fotmob.Model;
using NoMoreBets.Features.Fotmob.Scraping;
using NoMoreBets.Features.MatchAnalysis.MatchMatcher;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.Fotmob.RefreshLeagueTableSnapshot;

/// <summary>
/// Handles <see cref="RefreshFotmobLeagueTableSnapshotCommand"/> by scraping FotMob table and xG stats,
/// merging by team name, and upserting into <see cref="LeagueTableSnapshot"/>.
/// </summary>
public class RefreshFotmobLeagueTableSnapshotHandler(
  IFotmobScraper scraper,
  AppDbContext db,
  IMatchMatcher matchMatcher,
  ILogger<RefreshFotmobLeagueTableSnapshotHandler> logger) : IRequestHandler<RefreshFotmobLeagueTableSnapshotCommand, Unit>
{
  /// <inheritdoc />
  public async Task<Unit> Handle(RefreshFotmobLeagueTableSnapshotCommand request, CancellationToken cancellationToken)
  {
    var seasonId = await db.Season
      .Where(s => s.LeagueId == request.LeagueId)
      .MaxAsync(s => (int?)s.Id, cancellationToken)
      .ConfigureAwait(false);

    if (seasonId == null)
    {
      throw new InvalidOperationException($"No season found for league {request.LeagueId}.");
    }

    var snapshotDate = DateOnly.FromDateTime(DateTime.UtcNow);
    var snapshotExists = await db.LeagueTableSnapshot
      .AnyAsync(s => s.SeasonId == seasonId && s.SnapshotDate == snapshotDate, cancellationToken)
      .ConfigureAwait(false);

    if (snapshotExists)
    {
      return Unit.Value;
    }

    var domainClubs = await db.Club
      .Where(c => c.LeagueId == request.LeagueId)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var tableTask = scraper.GetLeagueTableAsync(TableFilter.All, cancellationToken);
    var xgTask = scraper.GetXgStatsAsync(cancellationToken);
    await Task.WhenAll(tableTask, xgTask).ConfigureAwait(false);

    var tableClubs = tableTask.Result;
    var xgStats = xgTask.Result;

    var xgStatsDtos = xgStats.Select(XgStatsDto.From).ToList();

    var latestSnapshot = await db.LeagueTableSnapshot
      .Where(s => s.SeasonId == seasonId)
      .Include(s => s.Rows)
      .OrderByDescending(s => s.CreatedAt)
      .FirstOrDefaultAsync(cancellationToken) ?? new();

    if (latestSnapshot.Rows.Count > 0 && latestSnapshot.Rows.Count == tableClubs.Count)
    {
      var allMatchesPlayedUnchanged = tableClubs.All(readRecord =>
      {
        var domainClub = matchMatcher.FindClub(readRecord.TeamName, domainClubs);
        if (domainClub == null)
        {
          return false;
        }
        var previousRow = latestSnapshot.Rows.FirstOrDefault(r => r.ClubId == domainClub.Id);
        return previousRow != null && previousRow.MatchesPlayed == readRecord.MatchesPlayed;
      });

      if (allMatchesPlayedUnchanged)
      {
        return Unit.Value;
      }
    }

    var snapshot = new LeagueTableSnapshot
    {
      LeagueId = request.LeagueId,
      SeasonId = seasonId.Value,
      SnapshotDate = snapshotDate,
      CreatedAt = DateTime.UtcNow
    };

    foreach (var club in tableClubs)
    {
      var domainClub = matchMatcher.FindClub(club.TeamName, domainClubs);
      var xg = matchMatcher.FindXgStats(club.TeamName, xgStatsDtos);
      var row = new LeagueTableSnapshotRow
      {
        ClubId = domainClub.Id,
        Position = club.Position,
        MatchesPlayed = club.MatchesPlayed,
        Wins = club.Wins,
        Draws = club.Draws,
        Losses = club.Losses,
        GoalsFor = club.GoalsFor,
        GoalsAgainst = club.GoalsAgainst,
        GoalDifference = ParseGoalDifference(club.GoalDifference),
        Points = club.Points,
        Xg = (decimal?)xg?.Xg ?? 0,
        XgDiff = ParseDiffDecimal(xg?.XgDiff),
        Xga = (decimal?)xg?.Xga ?? 0,
        XgaDiff = ParseDiffDecimal(xg?.XgaDiff),
        Xpts = (decimal?)xg?.Xpts ?? 0,
        XptsDiff = ParseDiffDecimal(xg?.XptsDiff)
      };

      snapshot.Rows.Add(row);
    }

    db.LeagueTableSnapshot.Add(snapshot);
    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return Unit.Value;
  }

  private static int ParseGoalDifference(string value)
  {
    var s = (value ?? string.Empty).Trim();
    if (string.IsNullOrEmpty(s))
    {
      return 0;
    }

    return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
      ? n
      : 0;
  }

  private static decimal ParseDiffDecimal(string? value)
  {
    var s = (value ?? string.Empty).Trim();
    if (string.IsNullOrEmpty(s))
    {
      return 0;
    }

    return decimal.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d)
      ? d
      : 0;
  }
}
