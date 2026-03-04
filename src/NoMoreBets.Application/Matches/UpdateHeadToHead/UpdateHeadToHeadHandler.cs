using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.UpdateHeadToHead;

/// <summary>Command to refresh head-to-head data from SoccerData API and upsert into the database.</summary>
public record UpdateHeadToHeadCommand(int Team1SoccerdataId, int Team2SoccerdataId) : IRequest<Unit>;

public class UpdateHeadToHeadHandler(
  IHeadToHeadProvider headToHeadProvider,
  IUnitOfWork unitOfWork,
  ILogger<UpdateHeadToHeadHandler> logger) : IRequestHandler<UpdateHeadToHeadCommand, Unit>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public async Task<Unit> Handle(UpdateHeadToHeadCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation(
      "Handling {HandlerName} for Soccerdata teams {Team1SoccerdataId} vs {Team2SoccerdataId}",
      nameof(UpdateHeadToHeadHandler),
      request.Team1SoccerdataId,
      request.Team2SoccerdataId);

    var headToHead = await headToHeadProvider.GetHeadToHeadAsync(request.Team1SoccerdataId, request.Team2SoccerdataId);
    var clubs = await unitOfWork.Clubs.GetBySoccerdataId(
    [
      request.Team1SoccerdataId,
      request.Team2SoccerdataId
    ]);

    var club1 = clubs.FirstOrDefault(c => c.SoccerdataId == request.Team1SoccerdataId);
    var club2 = clubs.FirstOrDefault(c => c.SoccerdataId == request.Team2SoccerdataId);

    if (club1 == null || club2 == null)
    {
      logger.LogWarning(
        "Handler {HandlerName} cannot update head-to-head: missing clubs for Soccerdata ids {Team1SoccerdataId} and/or {Team2SoccerdataId}",
        nameof(UpdateHeadToHeadHandler),
        request.Team1SoccerdataId,
        request.Team2SoccerdataId);
      return Unit.Value;
    }

    var (team1DbId, team2DbId) = Head2Head.NormalizeClubIds(club1.Id, club2.Id);

    var head2HeadJson = JsonSerializer.Serialize(headToHead, JsonOptions);
    var entity = await unitOfWork.Matches.GetHeadToHead(team1DbId, team2DbId);

    if (entity == null)
    {
      entity = new Head2Head
      {
        Team1Id = team1DbId,
        Team2Id = team2DbId,
        Head2HeadJson = head2HeadJson,
        UpdatedAt = DateTime.UtcNow
      };
      await unitOfWork.Clubs.AddHead2Head(entity);

      logger.LogInformation(
        "Handler {HandlerName} created new Head2Head entry for clubs {Team1Id} vs {Team2Id}",
        nameof(UpdateHeadToHeadHandler),
        team1DbId,
        team2DbId);
    }
    else
    {
      entity.Head2HeadJson = head2HeadJson;
      entity.UpdatedAt = DateTime.UtcNow;

      logger.LogInformation(
        "Handler {HandlerName} updated existing Head2Head entry for clubs {Team1Id} vs {Team2Id}",
        nameof(UpdateHeadToHeadHandler),
        entity.Team1Id,
        entity.Team2Id);
    }

    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    return Unit.Value;
  }
}
