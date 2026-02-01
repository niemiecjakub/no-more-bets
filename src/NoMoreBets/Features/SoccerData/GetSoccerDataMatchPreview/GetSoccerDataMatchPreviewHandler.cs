using MediatR;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;

/// <summary>Handles <see cref="GetSoccerDataMatchPreviewQuery"/> by delegating to <see cref="ISoccerDataClient"/>.</summary>
public class GetSoccerDataMatchPreviewHandler(ISoccerDataClient client) : IRequestHandler<GetSoccerDataMatchPreviewQuery, MatchPreview>
{
    /// <inheritdoc />
    public Task<MatchPreview> Handle(GetSoccerDataMatchPreviewQuery request, CancellationToken cancellationToken) =>
        client.GetMatchPreviewAsync(request.MatchId, cancellationToken);
}
