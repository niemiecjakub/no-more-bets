using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Clubs.GetClubNextMatch;

public record GetClubNextMatchQuery(int ClubId) : IRequest<ClubNextMatchDto?>;

public sealed class GetClubNextMatchHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetClubNextMatchQuery, ClubNextMatchDto?>
{
  public async Task<ClubNextMatchDto?> Handle(
    GetClubNextMatchQuery request,
    CancellationToken cancellationToken)
  {
    var club = await unitOfWork.Clubs
      .GetByIdAsync(request.ClubId, cancellationToken)
      .ConfigureAwait(false);

    if (club == null)
      return null;

    var match = await unitOfWork.Matches
      .GetNextUpcomingMatchForClubAsync(request.ClubId, cancellationToken)
      .ConfigureAwait(false);

    if (match == null)
      return null;

    var isHome = match.HomeClubId == request.ClubId;

    return new ClubNextMatchDto(
      match.Id,
      match.MatchDate,
      match.HomeClubId,
      match.AwayClubId,
      match.HomeClub.Name,
      match.AwayClub.Name,
      match.HomeClub.Slug,
      match.AwayClub.Slug,
      isHome);
  }
}
