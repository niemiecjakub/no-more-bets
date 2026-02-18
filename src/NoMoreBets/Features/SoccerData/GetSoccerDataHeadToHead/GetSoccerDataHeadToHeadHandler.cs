using MediatR;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;

public class GetSoccerDataHeadToHeadHandler(ISoccerDataClient client) : IRequestHandler<GetSoccerDataHeadToHeadQuery, HeadToHead>
{
  public Task<HeadToHead> Handle(GetSoccerDataHeadToHeadQuery request, CancellationToken cancellationToken)
  {
    return client.GetHeadToHeadAsync(request.Team1Id, request.Team2Id, cancellationToken);
  }
}
