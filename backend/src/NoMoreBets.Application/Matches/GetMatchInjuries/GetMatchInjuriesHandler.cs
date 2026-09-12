using MediatR;
using NoMoreBets.Domain.Matches;
using Microsoft.Extensions.Logging;

namespace NoMoreBets.Application.Matches.GetMatchInjuries;

public record GetMatchInjuriesQuery(int MatchId) : IRequest<MatchInjuriesResult?>;

public sealed class GetMatchInjuriesHandler(IMatchRepository matches, ILogger<GetMatchInjuriesHandler>? logger = null) : IRequestHandler<GetMatchInjuriesQuery, MatchInjuriesResult?>
{
  public async Task<MatchInjuriesResult?> Handle(GetMatchInjuriesQuery request, CancellationToken cancellationToken)
  {
    var lineup = await matches.GetLineup(request.MatchId).ConfigureAwait(false);
    if (lineup == null)
    {
      logger?.LogWarning("No injuries data found because lineup is missing for match {MatchId}.", request.MatchId);
      return null;
    }

    var homeLineup = lineup.GetHomeTeamLineup();
    var awayLineup = lineup.GetAwayTeamLineup();

    return new MatchInjuriesResult(
      Home: new TeamInjuriesResult(homeLineup.Injuries.Select(p => new InjuriedPlayer(p.Player, p.Position.ToString(), p.Status.ToString())).ToList()),
      Away: new TeamInjuriesResult(awayLineup.Injuries.Select(p => new InjuriedPlayer(p.Player, p.Position.ToString(), p.Status.ToString())).ToList()));
  }
}
