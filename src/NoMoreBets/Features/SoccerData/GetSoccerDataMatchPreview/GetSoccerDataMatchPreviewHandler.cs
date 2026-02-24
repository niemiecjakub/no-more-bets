using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Features.SoccerData.Model;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;

/// <summary>Query to fetch a single match preview from the database (cached). Returns null if not found.</summary>
public record GetSoccerDataMatchPreviewQuery(int MatchId) : IRequest<MatchPreview?>;

public class GetSoccerDataMatchPreviewHandler(AppDbContext db) : IRequestHandler<GetSoccerDataMatchPreviewQuery, MatchPreview?>
{
  public async Task<MatchPreview?> Handle(GetSoccerDataMatchPreviewQuery request, CancellationToken cancellationToken)
  {
    var match = await db.Match
      .FirstOrDefaultAsync(m => m.SoccerdataId == request.MatchId, cancellationToken)
      .ConfigureAwait(false);

    if (match == null)
      return null;

    var entity = await db.MatchPreview
      .FirstOrDefaultAsync(e => e.MatchId == match.Id, cancellationToken)
      .ConfigureAwait(false);

    if (entity == null)
      return null;

    return new MatchPreview
    {
      Id = request.MatchId,
      PreviewContent = entity.GetPreviewContent()
    };
  }
}
