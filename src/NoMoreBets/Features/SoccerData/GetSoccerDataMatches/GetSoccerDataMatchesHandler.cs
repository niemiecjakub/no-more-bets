using MediatR;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatches;

/// <summary>Handles <see cref="GetSoccerDataMatchesQuery"/> by delegating to <see cref="ISoccerDataClient"/>.</summary>
public class GetSoccerDataMatchesHandler(ISoccerDataClient client) : IRequestHandler<GetSoccerDataMatchesQuery, IReadOnlyList<LeagueMatches>>
{
    /// <inheritdoc />
    public Task<IReadOnlyList<LeagueMatches>> Handle(GetSoccerDataMatchesQuery request, CancellationToken cancellationToken) =>
        client.GetMatchesAsync(request.Date, request.LeagueId, request.Season, cancellationToken);
}
