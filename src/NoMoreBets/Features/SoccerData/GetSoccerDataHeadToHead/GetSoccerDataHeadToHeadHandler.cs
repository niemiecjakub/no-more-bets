using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Entity;
using NoMoreBets.Features.SoccerData.Model;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;

public class GetSoccerDataHeadToHeadHandler(
  ISoccerDataClient client,
  AppDbContext db) : IRequestHandler<GetSoccerDataHeadToHeadQuery, HeadToHead>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public async Task<HeadToHead> Handle(GetSoccerDataHeadToHeadQuery request, CancellationToken cancellationToken)
  {
    var headToHead = await client.GetHeadToHeadAsync(request.Team1Id, request.Team2Id, cancellationToken).ConfigureAwait(false);

    var clubIds = new[] { request.Team1Id, request.Team2Id };
    var clubs = await db.Club
      .Where(c => clubIds.Contains(c.SoccerdataId))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var club1 = clubs.FirstOrDefault(c => c.SoccerdataId == request.Team1Id);
    var club2 = clubs.FirstOrDefault(c => c.SoccerdataId == request.Team2Id);

    if (club1 != null && club2 != null)
    {
      var team1DbId = Math.Min(club1.Id, club2.Id);
      var team2DbId = Math.Max(club1.Id, club2.Id);

      var head2HeadJson = JsonSerializer.Serialize(headToHead, JsonOptions);
      var entity = await db.Head2Head
        .FirstOrDefaultAsync(e => e.Team1Id == team1DbId && e.Team2Id == team2DbId, cancellationToken)
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

    return headToHead;
  }
}
