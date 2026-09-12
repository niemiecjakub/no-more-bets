using MediatR;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto.Clubs;
using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Application.Clubs.GetClubsList;

public record GetClubsListQuery : IRequest<IReadOnlyList<ClubDto>>;

public sealed class GetClubsListHandler(IClubRepository clubs)
  : IRequestHandler<GetClubsListQuery, IReadOnlyList<ClubDto>>
{
  public async Task<IReadOnlyList<ClubDto>> Handle(
    GetClubsListQuery request,
    CancellationToken cancellationToken)
  {
    var clubList = await clubs
      .GetClubsWithMembershipsOrderedByNameAsync(cancellationToken)
      .ConfigureAwait(false);

    return clubList
      .Select(c => new ClubDto(
        c.Id,
        c.Name,
        c.Slug,
        c.ClubSeasons
          .Where(cs => cs.Season.League.Slug != League.UnknownSlug)
          .OrderByDescending(cs => cs.Season.StartDate ?? DateOnly.MinValue)
          .ThenByDescending(cs => cs.Season.Year)
          .ThenByDescending(cs => cs.Season.Id)
          .Select(cs => new ClubSeasonMembershipDto(
            cs.SeasonId,
            cs.Season.Year,
            cs.Season.StartDate,
            cs.Season.EndDate,
            cs.Season.LeagueId,
            cs.Season.League.Name,
            cs.Season.League.Slug))
          .ToList()))
      .ToList();
  }
}
