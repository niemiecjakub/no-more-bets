using MediatR;
using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Application.Leagues.GetLeaguesList;

public record GetLeaguesListQuery : IRequest<IReadOnlyList<LeagueDto>>;

public sealed class GetLeaguesListHandler(ILeagueRepository leagues)
  : IRequestHandler<GetLeaguesListQuery, IReadOnlyList<LeagueDto>>
{
  public async Task<IReadOnlyList<LeagueDto>> Handle(
    GetLeaguesListQuery request,
    CancellationToken cancellationToken)
  {
    var leagueList = await leagues
      .GetLeaguesOrderedByNameAsync(cancellationToken)
      .ConfigureAwait(false);

    return leagueList
      .Select(l => new LeagueDto(l.Id, l.Name, l.Slug))
      .ToList();
  }
}
