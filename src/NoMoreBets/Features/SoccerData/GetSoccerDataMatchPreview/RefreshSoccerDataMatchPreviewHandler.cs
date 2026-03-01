using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Infrastructure.Database;
using MatchPreviewEntity = NoMoreBets.Domain.Matches.MatchPreview;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;

/// <summary>Command to refresh a single match preview from SoccerData API and upsert into the database.</summary>
public record RefreshSoccerDataMatchPreviewCommand(int SoccerdataMatchId) : IRequest<Unit>;

public class RefreshSoccerDataMatchPreviewHandler(
  SoccerDataClient client,
  AppDbContext db,
  ILogger<RefreshSoccerDataMatchPreviewHandler> logger) : IRequestHandler<RefreshSoccerDataMatchPreviewCommand, Unit>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public async Task<Unit> Handle(RefreshSoccerDataMatchPreviewCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation(
      "Handling {HandlerName} for Soccerdata match {SoccerdataMatchId}",
      nameof(RefreshSoccerDataMatchPreviewHandler),
      request.SoccerdataMatchId);

    var matchPreview = await client.GetMatchPreviewAsync(request.SoccerdataMatchId, cancellationToken).ConfigureAwait(false);

    var match = await db.Match
      .FirstOrDefaultAsync(m => m.SoccerdataId == request.SoccerdataMatchId, cancellationToken)
      .ConfigureAwait(false);

    if (match == null)
    {
      logger.LogWarning(
        "Handler {HandlerName} found no match in DB for Soccerdata match {SoccerdataMatchId}",
        nameof(RefreshSoccerDataMatchPreviewHandler),
        request.SoccerdataMatchId);
      return Unit.Value;
    }

    var previewContentJson = JsonSerializer.Serialize(matchPreview.PreviewContent, JsonOptions);
    var entity = await db.MatchPreview
      .FirstOrDefaultAsync(e => e.MatchId == match.Id, cancellationToken)
      .ConfigureAwait(false);

    if (entity == null)
    {
      entity = new MatchPreviewEntity
      {
        MatchId = match.Id,
        PreviewContentJson = previewContentJson
      };
      db.MatchPreview.Add(entity);

      logger.LogInformation(
        "Handler {HandlerName} created new match preview for MatchId={MatchId}, SoccerdataMatchId={SoccerdataMatchId}",
        nameof(RefreshSoccerDataMatchPreviewHandler),
        match.Id,
        request.SoccerdataMatchId);
    }
    else
    {
      entity.PreviewContentJson = previewContentJson;

      logger.LogInformation(
        "Handler {HandlerName} updated existing match preview for MatchId={MatchId}, SoccerdataMatchId={SoccerdataMatchId}",
        nameof(RefreshSoccerDataMatchPreviewHandler),
        match.Id,
        request.SoccerdataMatchId);
    }

    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    return Unit.Value;
  }
}
