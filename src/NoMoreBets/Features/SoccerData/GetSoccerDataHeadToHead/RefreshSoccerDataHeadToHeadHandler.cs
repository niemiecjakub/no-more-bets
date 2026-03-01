using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;

/// <summary>Command to refresh head-to-head data from SoccerData API and upsert into the database.</summary>
public record RefreshSoccerDataHeadToHeadCommand(int Team1SoccerdataId, int Team2SoccerdataId) : IRequest<Unit>;

public class RefreshSoccerDataHeadToHeadHandler(
  SoccerDataClient client,
  AppDbContext db,
  ILogger<RefreshSoccerDataHeadToHeadHandler> logger) : IRequestHandler<RefreshSoccerDataHeadToHeadCommand, Unit>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public async Task<Unit> Handle(RefreshSoccerDataHeadToHeadCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation(
      "Handling {HandlerName} for Soccerdata teams {Team1SoccerdataId} vs {Team2SoccerdataId}",
      nameof(RefreshSoccerDataHeadToHeadHandler),
      request.Team1SoccerdataId,
      request.Team2SoccerdataId);

    var headToHead = await client.GetHeadToHeadAsync(request.Team1SoccerdataId, request.Team2SoccerdataId, cancellationToken).ConfigureAwait(false);

    var clubIds = new[] { request.Team1SoccerdataId, request.Team2SoccerdataId };
    var clubs = await db.Club
      .Where(c => clubIds.Contains(c.SoccerdataId))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var club1 = clubs.FirstOrDefault(c => c.SoccerdataId == request.Team1SoccerdataId);
    var club2 = clubs.FirstOrDefault(c => c.SoccerdataId == request.Team2SoccerdataId);

    if (club1 == null || club2 == null)
    {
      logger.LogWarning(
        "Handler {HandlerName} cannot update head-to-head: missing clubs for Soccerdata ids {Team1SoccerdataId} and/or {Team2SoccerdataId}",
        nameof(RefreshSoccerDataHeadToHeadHandler),
        request.Team1SoccerdataId,
        request.Team2SoccerdataId);
      return Unit.Value;
    }

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

      logger.LogInformation(
        "Handler {HandlerName} created new Head2Head entry for clubs {Team1Id} vs {Team2Id}",
        nameof(RefreshSoccerDataHeadToHeadHandler),
        team1DbId,
        team2DbId);
    }
    else
    {
      entity.Head2HeadJson = head2HeadJson;
      entity.UpdatedAt = DateTime.UtcNow;

      logger.LogInformation(
        "Handler {HandlerName} updated existing Head2Head entry for clubs {Team1Id} vs {Team2Id}",
        nameof(RefreshSoccerDataHeadToHeadHandler),
        entity.Team1Id,
        entity.Team2Id);
    }

    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    return Unit.Value;
  }
}
