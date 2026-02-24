using MediatR;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatches;

/// <summary>Query to fetch matches from SoccerData API by date, league, and/or season.</summary>
public record GetSoccerDataMatchesQuery(string? Date = null, int? LeagueId = null, string? Season = null) : IRequest<IReadOnlyList<LeagueMatches>>;

public class GetSoccerDataMatchesHandler(SoccerDataClient client) : IRequestHandler<GetSoccerDataMatchesQuery, IReadOnlyList<LeagueMatches>>
{
  public Task<IReadOnlyList<LeagueMatches>> Handle(GetSoccerDataMatchesQuery request, CancellationToken cancellationToken)
  {
    return client.GetMatchesAsync(request.Date, request.LeagueId, request.Season, cancellationToken);
  }
}
