using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Application.Leagues.GetLeagueTableDisplay;

public record GetLeagueTableDisplayQuery(int LeagueId) : IRequest<LeagueTableDto?>;

public sealed class GetLeagueTableDisplayHandler(IUnitOfWork unitOfWork)
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

    return MapToDto(snapshot, formByClub);
  }

  private static LeagueTableDto MapToDto(
    LeagueTableSnapshot snapshot,
    IReadOnlyDictionary<int, IReadOnlyList<MatchResult>> formByClub)
  {
    var rows = snapshot.Rows
      .OrderBy(r => r.Position)
      .Select(r => new LeagueTableRowDto(
        r.Position,
        r.ClubId,
        r.Club.Name,
        r.Club.Slug,
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
        r.XptsDiff,
        formByClub.GetValueOrDefault(r.ClubId, Array.Empty<MatchResult>())))
      .ToList();

    return new LeagueTableDto(
      snapshot.Id,
      snapshot.LeagueId,
      snapshot.SeasonId,
      snapshot.SnapshotDate,
      snapshot.League.Name,
      snapshot.League.Slug,
      rows);
  }
}
