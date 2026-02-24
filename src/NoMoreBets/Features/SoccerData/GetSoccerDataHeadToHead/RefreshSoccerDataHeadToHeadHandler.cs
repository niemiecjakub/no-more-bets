using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Entity;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;

public class RefreshSoccerDataHeadToHeadHandler(
  SoccerDataClient client,
  AppDbContext db) : IRequestHandler<RefreshSoccerDataHeadToHeadCommand, Unit>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public async Task<Unit> Handle(RefreshSoccerDataHeadToHeadCommand request, CancellationToken cancellationToken)
  {
    var headToHead = await client.GetHeadToHeadAsync(request.Team1SoccerdataId, request.Team2SoccerdataId, cancellationToken).ConfigureAwait(false);

    var clubIds = new[] { request.Team1SoccerdataId, request.Team2SoccerdataId };
    var clubs = await db.Club
      .Where(c => clubIds.Contains(c.SoccerdataId))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var club1 = clubs.FirstOrDefault(c => c.SoccerdataId == request.Team1SoccerdataId);
    var club2 = clubs.FirstOrDefault(c => c.SoccerdataId == request.Team2SoccerdataId);

    if (club1 != null && club2 != null)
    {
      var (team1DbId, team2DbId) = Head2Head.NormalizeClubIds(club1.Id, club2.Id);

      var head2HeadJson = JsonSerializer.Serialize(headToHead, JsonOptions);
      var entity = await db.Head2Head
        .ForClubs(club1.Id, club2.Id)
        .FirstOrDefaultAsync(cancellationToken)
        .ConfigureAwait(false);

      if (entity == null)
      {
        entity = new Head2Head
        {
          Team1Id = team1DbId,
          Team2Id = team2DbId,
          Head2HeadJson = head2HeadJson,
          UpdatedAt = DateTime.UtcNow
        };
        db.Head2Head.Add(entity);
      }
      else
      {
        entity.Head2HeadJson = head2HeadJson;
        entity.UpdatedAt = DateTime.UtcNow;
      }

      await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    return Unit.Value;
  }
}
