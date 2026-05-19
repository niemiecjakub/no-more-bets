using MediatR;
using NoMoreBets.Application.Common;

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
      club.LeagueId,
      club.League.Name,
      club.Slug,
      club.League.Slug);
  }
}
