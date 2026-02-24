using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Features.SoccerData.Model;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;

/// <summary>Query to fetch head-to-head data between two teams from the database (cached). Returns null if not found.</summary>
public record GetSoccerDataHeadToHeadQuery(int Team1Id, int Team2Id) : IRequest<HeadToHead?>;

public class GetSoccerDataHeadToHeadHandler(AppDbContext db) : IRequestHandler<GetSoccerDataHeadToHeadQuery, HeadToHead?>
{
  public async Task<HeadToHead?> Handle(GetSoccerDataHeadToHeadQuery request, CancellationToken cancellationToken)
  {
    var clubIds = new[] { request.Team1Id, request.Team2Id };
    var clubs = await db.Club
      .Where(c => clubIds.Contains(c.SoccerdataId))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var club1 = clubs.FirstOrDefault(c => c.SoccerdataId == request.Team1Id);
    var club2 = clubs.FirstOrDefault(c => c.SoccerdataId == request.Team2Id);

    if (club1 == null || club2 == null)
      return null;

    var team1DbId = Math.Min(club1.Id, club2.Id);
    var team2DbId = Math.Max(club1.Id, club2.Id);

    var entity = await db.Head2Head
      .FirstOrDefaultAsync(e => e.Team1Id == team1DbId && e.Team2Id == team2DbId, cancellationToken)
      .ConfigureAwait(false);

    if (entity == null)
      return null;

    return entity.GetHeadToHead(request.Team1Id, request.Team2Id);
  }
}
