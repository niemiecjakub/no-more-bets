using MediatR;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatches;

public class GetSoccerDataMatchesHandler(ISoccerDataClient client) : IRequestHandler<GetSoccerDataMatchesQuery, IReadOnlyList<LeagueMatches>>
{
  public Task<IReadOnlyList<LeagueMatches>> Handle(GetSoccerDataMatchesQuery request, CancellationToken cancellationToken)
  {
    return client.GetMatchesAsync(request.Date, request.LeagueId, request.Season, cancellationToken);
  }
}
