using MediatR;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;

/// <summary>Handles <see cref="GetSoccerDataMatchPreviewsUpcomingQuery"/> by delegating to <see cref="ISoccerDataClient"/>.</summary>
public class GetSoccerDataMatchPreviewsUpcomingHandler(ISoccerDataClient client) : IRequestHandler<GetSoccerDataMatchPreviewsUpcomingQuery, IReadOnlyList<LeagueMatchPreviews>>
{
    /// <inheritdoc />
    public Task<IReadOnlyList<LeagueMatchPreviews>> Handle(GetSoccerDataMatchPreviewsUpcomingQuery request, CancellationToken cancellationToken) =>
        client.GetMatchPreviewsUpcomingAsync(request.LeagueId, cancellationToken);
}
