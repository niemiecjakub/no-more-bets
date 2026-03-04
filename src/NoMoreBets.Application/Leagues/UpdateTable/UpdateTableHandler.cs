using System.Globalization;
using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Application.Common.Dto.Clubs;
using NoMoreBets.Application.Leagues;
using NoMoreBets.Application.Common.Dto.Leagues;
using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Application.Leagues.UpdateTable;

/// <summary>Command to refresh league table snapshot from FotMob (scrape table + xG, merge, persist). Always updates the latest season (max id) for the given league.</summary>
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

    var season = await unitOfWork.Leagues.GetLatestSeason(request.LeagueId);

    if (season == null)
    {
      logger.LogError(
        "Handler {HandlerName} found no season for league {LeagueId}",
        nameof(UpdateTableHandler),
        request.LeagueId);
      throw new InvalidOperationException($"No season found for league {request.LeagueId}.");
    }

    var snapshotDate = DateOnly.FromDateTime(DateTime.UtcNow);
    var snapshotExists = await unitOfWork.Leagues.TableSnapshotExists(request.LeagueId, snapshotDate);

    if (snapshotExists)
    {
      logger.LogInformation(
        "Handler {HandlerName} skipping snapshot creation because snapshot already exists for league {LeagueId} on {SnapshotDate}",
        nameof(UpdateTableHandler),
        request.LeagueId,
        snapshotDate);
      return Unit.Value;
    }

    var domainClubs = await unitOfWork.Clubs.GetClubs(request.LeagueId);

    var tableTask = leagueProvider.GetLeagueTableAsync();
    var xgTask = leagueProvider.GetXgStatsAsync();
    await Task.WhenAll(tableTask, xgTask).ConfigureAwait(false);

    var tableClubs = tableTask.Result;
    var xgStats = xgTask.Result;

    var xgStatsDtos = xgStats.Select(XgStatsDto.From).ToList();

    var latestSnapshot = await unitOfWork.Leagues.GetLatestTableSnapshot(request.LeagueId) ?? new();

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
