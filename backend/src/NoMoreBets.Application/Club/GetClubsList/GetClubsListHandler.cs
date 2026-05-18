using MediatR;
using NoMoreBets.Application.Common;

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
      .GetClubsWithLeagueOrderedByNameAsync(cancellationToken)
      .ConfigureAwait(false);

    return clubs
      .Select(c => new ClubDto(c.Id, c.Name, c.LeagueId, c.League.Name, c.Slug, c.League.Slug))
      .ToList();
  }
}
