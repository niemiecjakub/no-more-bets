using MediatR;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;

public class GetSoccerDataMatchPreviewHandler(ISoccerDataClient client) : IRequestHandler<GetSoccerDataMatchPreviewQuery, MatchPreview>
{
  public Task<MatchPreview> Handle(GetSoccerDataMatchPreviewQuery request, CancellationToken cancellationToken)
  {
    return client.GetMatchPreviewAsync(request.MatchId, cancellationToken);
  }
}
