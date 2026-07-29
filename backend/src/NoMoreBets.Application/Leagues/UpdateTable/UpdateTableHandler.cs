using System.Globalization;
using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto.Leagues;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Application.Leagues;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Application.Leagues.UpdateTable;

/// <summary>Command to refresh the active league season's table snapshot from FotMob.</summary>
public record UpdateTableCommand(int LeagueId) : IRequest<Unit>;

/// <summary>
/// Handles <see cref="UpdateTableCommand"/> by scraping FotMob table and xG stats,
/// merging by team name, and upserting into <see cref="LeagueTableSnapshot"/>.
/// </summary>
public class UpdateTableHandler(
  ILeagueProvider leagueProvider,
  IUnitOfWork unitOfWork,
  IMatchMatcher matchMatcher,
  ILogger<UpdateTableHandler> logger) : IRequestHandler<UpdateTableCommand, Unit>
{
  /// <inheritdoc />
  public async Task<Unit> Handle(UpdateTableCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation(
      "Handling {HandlerName} for league {LeagueId}",
      nameof(UpdateTableHandler),
      request.LeagueId);

    var snapshotDate = DateOnly.FromDateTime(DateTime.UtcNow);
    var season = await unitOfWork.Leagues.GetLatestSeasonAsync(request.LeagueId, cancellationToken);

    if (season == null)
    {
      logger.LogInformation(
        "Handler {HandlerName} skipped league {LeagueId} because no season exists",
        nameof(UpdateTableHandler),
        request.LeagueId);
      return Unit.Value;
    }

    if (!SeasonFetchWindow.Contains(season, snapshotDate))
    {
      logger.LogInformation(
        "Handler {HandlerName} skipped league {LeagueId}: latest season {SeasonId} ({StartDate}–{EndDate}) is outside the ±{WindowDays}d fetch window on {SnapshotDate}",
        nameof(UpdateTableHandler),
        request.LeagueId,
        season.Id,
        season.StartDate,
        season.EndDate,
        SeasonFetchWindow.Days,
        snapshotDate);
      return Unit.Value;
    }

    var snapshotExists = await unitOfWork.Leagues.TableSnapshotExists(season.Id, snapshotDate);

    if (snapshotExists)
    {
      logger.LogInformation(
        "Handler {HandlerName} skipping snapshot creation because snapshot already exists for league {LeagueId} on {SnapshotDate}",
        nameof(UpdateTableHandler),
        request.LeagueId,
        snapshotDate);
      return Unit.Value;
    }

    var domainClubs = await unitOfWork.Clubs.GetClubsForSeasonAsync(season.Id);
    var league = (await unitOfWork.Leagues.GetLeagues())
      .FirstOrDefault(l => l.Id == request.LeagueId);
    if (league == null)
    {
      logger.LogError(
        "Handler {HandlerName} found no league row for league {LeagueId}",
        nameof(UpdateTableHandler),
        request.LeagueId);
      throw new InvalidOperationException($"No league found for id {request.LeagueId}.");
    }

    var tableTask = leagueProvider.GetLeagueTableAsync(league.Slug);
    var xgTask = leagueProvider.GetXgStatsAsync(league.Slug);
    await Task.WhenAll(tableTask, xgTask).ConfigureAwait(false);

    var tableClubs = tableTask.Result;
    var xgStats = xgTask.Result;

    EnsureCompleteSeasonTableData(request.LeagueId, domainClubs, tableClubs, xgStats);

    var latestSnapshot = await unitOfWork.Leagues.GetLatestTableSnapshot(season.Id) ?? new();

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
        logger.LogInformation(
          "Handler {HandlerName} skipping snapshot creation because all matches played are unchanged for league {LeagueId}",
          nameof(UpdateTableHandler),
          request.LeagueId);
        return Unit.Value;
      }
    }

    var snapshot = new LeagueTableSnapshot
    {
      LeagueId = request.LeagueId,
      SeasonId = season.Id,
      SnapshotDate = snapshotDate
    };

    foreach (var club in tableClubs)
    {
      var domainClub = matchMatcher.FindClub(club.TeamName, domainClubs);
      var xg = matchMatcher.FindXgStats(club.TeamName, xgStats);
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

    await unitOfWork.Leagues.AddLeagueTableSnapshot(snapshot);
    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation(
      "Handler {HandlerName} created league table snapshot for league {LeagueId} on {SnapshotDate} with {RowCount} rows",
      nameof(UpdateTableHandler),
      request.LeagueId,
      snapshotDate,
      snapshot.Rows.Count);
    return Unit.Value;
  }

  private void EnsureCompleteSeasonTableData(
    int leagueId,
    IReadOnlyList<Club> seasonClubs,
    IReadOnlyList<TableEntry> tableClubs,
    IReadOnlyList<XgStats> xgStats)
  {
    if (seasonClubs.Count == 0)
    {
      throw new IncompleteLeagueTableDataException(
        leagueId,
        missingTableDataForClubs: ["(no clubs in active season)"],
        missingXgDataForClubs: Array.Empty<string>(),
        unmatchedTableTeams: Array.Empty<string>());
    }

    var tableByClubId = new Dictionary<int, TableEntry>();
    var unmatchedTableTeams = new List<string>();

    foreach (var entry in tableClubs)
    {
      try
      {
        var club = matchMatcher.FindClub(entry.TeamName, seasonClubs);
        if (!tableByClubId.TryAdd(club.Id, entry))
        {
          unmatchedTableTeams.Add($"{entry.TeamName} (duplicate match for {club.Name})");
        }
      }
      catch (ClubMatchNotFoundException)
      {
        unmatchedTableTeams.Add(entry.TeamName);
      }
    }

    var missingTableClubs = seasonClubs
      .Where(club => !tableByClubId.ContainsKey(club.Id))
      .Select(club => club.Name)
      .ToList();

    var missingXgClubs = seasonClubs
      .Where(club => tableByClubId.ContainsKey(club.Id))
      .Where(club => matchMatcher.FindXgStats(tableByClubId[club.Id].TeamName, xgStats) == null)
      .Select(club => club.Name)
      .ToList();

    if (missingTableClubs.Count == 0 && missingXgClubs.Count == 0 && unmatchedTableTeams.Count == 0)
    {
      return;
    }

    throw new IncompleteLeagueTableDataException(
      leagueId,
      missingTableClubs,
      missingXgClubs,
      unmatchedTableTeams);
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
