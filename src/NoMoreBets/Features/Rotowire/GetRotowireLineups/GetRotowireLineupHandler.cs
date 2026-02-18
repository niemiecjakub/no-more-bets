using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Features.Rotowire.Model;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.Rotowire.GetRotowireLineups;

public class GetRotowireLineupHandler(AppDbContext db) : IRequestHandler<GetRotowireLineupQuery, GameLineup?>
{
  public async Task<GameLineup?> Handle(GetRotowireLineupQuery request, CancellationToken cancellationToken)
  {
    var lineup = await db.Lineup
      .Include(l => l.Match)
      .ThenInclude(m => m.HomeClub)
      .Include(l => l.Match)
      .ThenInclude(m => m.AwayClub)
      .FirstOrDefaultAsync(l => l.Match.SoccerdataId == request.SoccerDataMatchId, cancellationToken)
      .ConfigureAwait(false);

    if (lineup == null)
      return null;

    return new GameLineup
    {
      Date = lineup.Match.MatchDate,
      Time = lineup.Match.MatchDate.ToString("HH:mm"),
      HomeTeamName = lineup.Match.HomeClub.Name,
      AwayTeamName = lineup.Match.AwayClub.Name,
      HomeTeam = lineup.GetHomeTeamLineup(),
      AwayTeam = lineup.GetAwayTeamLineup()
    };
  }
}
