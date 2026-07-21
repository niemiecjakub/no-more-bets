using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto.Clubs;
using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Application.Clubs.GetClubById;

public record GetClubByIdQuery(int ClubId) : IRequest<ClubDetailDto?>;

public sealed class GetClubByIdHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetClubByIdQuery, ClubDetailDto?>
{
  public async Task<ClubDetailDto?> Handle(
    GetClubByIdQuery request,
    CancellationToken cancellationToken)
  {
    var club = await unitOfWork.Clubs
      .GetByIdAsync(request.ClubId, cancellationToken)
      .ConfigureAwait(false);

    if (club == null)
      return null;

    return new ClubDetailDto(
      club.Id,
      club.Name,
      club.Slug,
      club.ClubSeasons
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
        .ToList());
  }
}
