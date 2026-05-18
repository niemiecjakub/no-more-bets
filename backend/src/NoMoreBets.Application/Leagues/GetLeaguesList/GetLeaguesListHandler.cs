using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Leagues.GetLeaguesList;

public record GetLeaguesListQuery : IRequest<IReadOnlyList<LeagueDto>>;

public sealed class GetLeaguesListHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetLeaguesListQuery, IReadOnlyList<LeagueDto>>
{
  public async Task<IReadOnlyList<LeagueDto>> Handle(
    GetLeaguesListQuery request,
    CancellationToken cancellationToken)
  {
    var leagues = await unitOfWork.Leagues
      .GetLeaguesOrderedByNameAsync(cancellationToken)
      .ConfigureAwait(false);

    return leagues
      .Select(l => new LeagueDto(l.Id, l.Name, l.Slug))
      .ToList();
  }
}
