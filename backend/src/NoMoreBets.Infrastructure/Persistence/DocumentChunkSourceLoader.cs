using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Infrastructure.Persistence;

public interface IDocumentChunkSourceLoader
{
  Task<IDocumentChunkSource?> LoadAsync(string sourceType, int sourceId, CancellationToken cancellationToken = default);
}

public sealed class DocumentChunkSourceLoader(AppDbContext db) : IDocumentChunkSourceLoader
{
  public async Task<IDocumentChunkSource?> LoadAsync(
    string sourceType,
    int sourceId,
    CancellationToken cancellationToken = default) =>
    sourceType switch
    {
      DocumentChunkSourceType.Match => await LoadMatchAsync(sourceId, cancellationToken).ConfigureAwait(false),
      DocumentChunkSourceType.MatchAnalysis => await LoadMatchAnalysisAsync(sourceId, cancellationToken).ConfigureAwait(false),
      _ => null
    };

  private async Task<IDocumentChunkSource?> LoadMatchAsync(int sourceId, CancellationToken cancellationToken)
  {
    return await db.Match
      .AsSplitQuery()
      .Include(m => m.HomeClub)
        .ThenInclude(c => c.League)
      .Include(m => m.AwayClub)
      .Include(m => m.Stage!)
        .ThenInclude(s => s.Season)
          .ThenInclude(s => s.League)
      .Include(m => m.Lineup)
      .Include(m => m.MatchEvents)
        .ThenInclude(e => e.Player)
      .FirstOrDefaultAsync(m => m.Id == sourceId, cancellationToken)
      .ConfigureAwait(false);
  }

  private async Task<IDocumentChunkSource?> LoadMatchAnalysisAsync(int sourceId, CancellationToken cancellationToken)
  {
    return await db.MatchAnalysis
      .AsSplitQuery()
      .Include(a => a.Match)
        .ThenInclude(m => m.HomeClub)
          .ThenInclude(c => c.League)
      .Include(a => a.Match)
        .ThenInclude(m => m.AwayClub)
      .Include(a => a.Match)
        .ThenInclude(m => m.Stage!)
          .ThenInclude(s => s.Season)
            .ThenInclude(s => s.League)
      .FirstOrDefaultAsync(a => a.Id == sourceId, cancellationToken)
      .ConfigureAwait(false);
  }
}
