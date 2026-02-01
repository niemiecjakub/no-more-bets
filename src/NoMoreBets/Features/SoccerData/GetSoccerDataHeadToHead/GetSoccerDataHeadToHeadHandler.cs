using MediatR;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;

/// <summary>Handles <see cref="GetSoccerDataHeadToHeadQuery"/> by delegating to <see cref="ISoccerDataClient"/>.</summary>
public class GetSoccerDataHeadToHeadHandler(ISoccerDataClient client) : IRequestHandler<GetSoccerDataHeadToHeadQuery, HeadToHead>
{
    /// <inheritdoc />
    public Task<HeadToHead> Handle(GetSoccerDataHeadToHeadQuery request, CancellationToken cancellationToken) =>
        client.GetHeadToHeadAsync(request.Team1Id, request.Team2Id, cancellationToken);
}
