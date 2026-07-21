using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto.Clubs;
using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Application.Clubs.GetClubsList;

public record GetClubsListQuery : IRequest<IReadOnlyList<ClubDto>>;

public sealed class GetClubsListHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetClubsListQuery, IReadOnlyList<ClubDto>>
{
  public async Task<IReadOnlyList<ClubDto>> Handle(
    GetClubsListQuery request,
    CancellationToken cancellationToken)
  {
    var clubs = await unitOfWork.Clubs
      .GetClubsWithMembershipsOrderedByNameAsync(cancellationToken)
      .ConfigureAwait(false);

    return clubs
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
