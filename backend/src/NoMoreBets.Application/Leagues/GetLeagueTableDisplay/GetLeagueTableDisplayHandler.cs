using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Application.Leagues.GetLeagueTableDisplay;

public record GetLeagueTableDisplayQuery(int LeagueId, int? ClubId = null) : IRequest<LeagueTableDto?>;

public sealed class GetLeagueTableDisplayHandler(
  IUnitOfWork unitOfWork,
  WorldCupGroupRegistry worldCupGroupRegistry)
  : IRequestHandler<GetLeagueTableDisplayQuery, LeagueTableDto?>
{
  public async Task<LeagueTableDto?> Handle(
    GetLeagueTableDisplayQuery request,
    CancellationToken cancellationToken)
  {
    var snapshot = await unitOfWork.Leagues
      .GetLatestLeagueTableSnapshotAsync(request.LeagueId, cancellationToken)
      .ConfigureAwait(false);

    if (snapshot == null)
      return null;

    var clubIds = snapshot.Rows.Select(r => r.ClubId).ToList();
    var formByClub = await unitOfWork.Matches
      .GetFormForClubsInSeasonAsync(snapshot.SeasonId, clubIds, 5, cancellationToken)
      .ConfigureAwait(false);

    var rowDtosByClubId = snapshot.Rows.ToDictionary(
      r => r.ClubId,
      r => MapRow(r, formByClub));

    if (request.ClubId is null
        || !worldCupGroupRegistry.IsWorldCupLeagueSlug(snapshot.League.Slug))
    {
      return MapFlatDto(snapshot, rowDtosByClubId.Values.OrderBy(r => r.Position).ToList());
    }

    var club = await unitOfWork.Clubs.GetByIdAsync(request.ClubId.Value, cancellationToken)
      .ConfigureAwait(false);
    if (club is null || club.LeagueId != request.LeagueId)
      return null;

    var ownGroup = worldCupGroupRegistry.GetGroupForClubName(club.Name);
    if (ownGroup is null)
      return MapFlatDto(snapshot, rowDtosByClubId.Values.OrderBy(r => r.Position).ToList());

    var orderedGroups = worldCupGroupRegistry.Groups
      .OrderBy(g => g.Code == ownGroup.Code ? 0 : 1)
      .ThenBy(g => g.Code, StringComparer.Ordinal)
      .ToList();

    var groups = orderedGroups
      .Select(group => new WorldCupGroupTableDto(
        group.Code,
        group.Label,
        snapshot.Rows
          .Where(r => worldCupGroupRegistry.IsClubInGroup(r.Club.Name, group.Code))
          .OrderBy(r => r.Position)
          .Select(r => rowDtosByClubId[r.ClubId])
          .ToList()))
      .ToList();

    var ownGroupRows = groups.First(g => g.GroupCode == ownGroup.Code).Rows;

    return new LeagueTableDto(
      snapshot.Id,
      snapshot.LeagueId,
      snapshot.SeasonId,
      snapshot.SnapshotDate,
      snapshot.League.Name,
      snapshot.League.Slug,
      ownGroupRows,
      ownGroup.Code,
      groups);
  }

  private static LeagueTableDto MapFlatDto(
    LeagueTableSnapshot snapshot,
    IReadOnlyList<LeagueTableRowDto> rows) =>
    new(
      snapshot.Id,
      snapshot.LeagueId,
      snapshot.SeasonId,
      snapshot.SnapshotDate,
      snapshot.League.Name,
      snapshot.League.Slug,
      rows);

  private static LeagueTableRowDto MapRow(
    LeagueTableSnapshotRow row,
    IReadOnlyDictionary<int, IReadOnlyList<MatchResult>> formByClub) =>
    new(
      row.Position,
      row.ClubId,
      row.Club.Name,
      row.Club.Slug,
      row.MatchesPlayed,
      row.Wins,
      row.Draws,
      row.Losses,
      row.GoalsFor,
      row.GoalsAgainst,
      row.GoalDifference,
      row.Points,
      row.Xg,
      row.XgDiff,
      row.Xga,
      row.XgaDiff,
      row.Xpts,
      row.XptsDiff,
      formByClub.GetValueOrDefault(row.ClubId, Array.Empty<MatchResult>()));
}
