using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Matches.GetMatchLineups;

public record GetMatchLineupsQuery(int MatchId) : IRequest<MatchLineupResult?>;

public sealed class GetMatchLineupsHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMatchLineupsQuery, MatchLineupResult?>
{
  public async Task<MatchLineupResult?> Handle(GetMatchLineupsQuery request, CancellationToken cancellationToken)
  {
    var lineup = await unitOfWork.Matches.GetLineup(request.MatchId).ConfigureAwait(false);
    if (lineup == null)
      return null;

    var homeLineup = lineup.GetHomeTeamLineup();
    var awayLineup = lineup.GetAwayTeamLineup();

    return new MatchLineupResult(
      Home: new TeamLineupResult(homeLineup.LineupType.ToString(), homeLineup.Players.Select(p => new Player(p.Player, p.Position.ToString())).ToList()),
      Away: new TeamLineupResult(awayLineup.LineupType.ToString(), awayLineup.Players.Select(p => new Player(p.Player, p.Position.ToString())).ToList()));
  }
}
