using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Infrastructure.Database;
using MatchPreviewEntity = NoMoreBets.Domain.Entity.MatchPreview;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;

public class RefreshSoccerDataMatchPreviewHandler(
  ISoccerDataClient client,
  AppDbContext db) : IRequestHandler<RefreshSoccerDataMatchPreviewCommand, Unit>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public async Task<Unit> Handle(RefreshSoccerDataMatchPreviewCommand request, CancellationToken cancellationToken)
  {
    var matchPreview = await client.GetMatchPreviewAsync(request.MatchId, cancellationToken).ConfigureAwait(false);

    var match = await db.Match
      .FirstOrDefaultAsync(m => m.SoccerdataId == request.MatchId, cancellationToken)
      .ConfigureAwait(false);

    if (match != null)
    {
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
      }
      else
      {
        entity.PreviewContentJson = previewContentJson;
      }

      await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    return Unit.Value;
  }
}
