using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Matches.GetMatchInjuries;

public record GetMatchInjuriesQuery(int MatchId) : IRequest<MatchInjuriesResult?>;

public sealed class GetMatchInjuriesHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMatchInjuriesQuery, MatchInjuriesResult?>
{
  public async Task<MatchInjuriesResult?> Handle(GetMatchInjuriesQuery request, CancellationToken cancellationToken)
  {
    var lineup = await unitOfWork.Matches.GetLineup(request.MatchId).ConfigureAwait(false);
    if (lineup == null)
      return null;

    var homeLineup = lineup.GetHomeTeamLineup();
    var awayLineup = lineup.GetAwayTeamLineup();

    return new MatchInjuriesResult(
      Home: new TeamInjuriesResult(homeLineup.Injuries.Select(p => new InjuriedPlayer(p.Player, p.Position.ToString(), p.Status.ToString())).ToList()),
      Away: new TeamInjuriesResult(awayLineup.Injuries.Select(p => new InjuriedPlayer(p.Player, p.Position.ToString(), p.Status.ToString())).ToList()));
  }
}
