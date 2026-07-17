using Microsoft.EntityFrameworkCore;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Persistence;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace NoMoreBets.Infrastructure.AI.Common.Embedding;

public sealed class DocumentChunkSearch(AppDbContext db) : IDocumentChunkSearch
{
  internal const double MaxCosineDistance = 0.55;
  internal const int CandidateChunkCeiling = 50;
  internal const int UniqueMatchCeiling = 200;
  internal const int RrfK = 60;

  public async Task<IReadOnlyList<int>> FindMatchIdsAsync(
    string query,
    float[] embedding,
    string embeddingModelId,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(query)
        || embedding.Length == 0
        || string.IsNullOrWhiteSpace(embeddingModelId))
      return [];

    var trimmed = query.Trim();
    var queryVector = new Vector(embedding);

    // DbContext is not concurrency-safe — run sequentially, not Task.WhenAll.
    var semantic = await SearchSemanticAsync(queryVector, embeddingModelId, cancellationToken)
      .ConfigureAwait(false);
    var lexical = await SearchLexicalAsync(trimmed, cancellationToken)
      .ConfigureAwait(false);

    var fused = FuseRrf(semantic, lexical);
    if (fused.Count == 0)
      return [];

    return await ResolveMatchIdsAsync(fused, cancellationToken).ConfigureAwait(false);
  }

  private async Task<IReadOnlyList<(string SourceType, int SourceId)>> SearchSemanticAsync(
    Vector queryVector,
    string embeddingModelId,
    CancellationToken cancellationToken)
  {
    var hits = await db.DocumentChunk
      .AsNoTracking()
      .Where(c => c.EmbeddingModel == embeddingModelId)
      .Where(c => c.Embedding.CosineDistance(queryVector) < MaxCosineDistance)
      .OrderBy(c => c.Embedding.CosineDistance(queryVector))
      .Take(CandidateChunkCeiling)
      .Select(c => new { c.SourceType, c.SourceId })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return hits
      .Select(h => (h.SourceType, h.SourceId))
      .ToList();
  }

  private async Task<IReadOnlyList<(string SourceType, int SourceId)>> SearchLexicalAsync(
    string query,
    CancellationToken cancellationToken)
  {
    // ponytail: on-the-fly to_tsvector — add a stored tsvector + GIN if this gets hot
    // PlainToTsQuery must stay inside the expression tree (not a local) or EF client-evals it.
    var rows = await db.DocumentChunk
      .AsNoTracking()
      .Where(c => EF.Functions.ToTsVector("english", c.Content)
        .Matches(EF.Functions.PlainToTsQuery("english", query)))
      .OrderByDescending(c => EF.Functions.ToTsVector("english", c.Content)
        .Rank(EF.Functions.PlainToTsQuery("english", query)))
      .Take(CandidateChunkCeiling)
      .Select(c => new { c.SourceType, c.SourceId })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return rows.Select(r => (r.SourceType, r.SourceId)).ToList();
  }

  internal static IReadOnlyList<(string SourceType, int SourceId)> FuseRrf(
    IReadOnlyList<(string SourceType, int SourceId)> semantic,
    IReadOnlyList<(string SourceType, int SourceId)> lexical)
  {
    var scores = new Dictionary<(string SourceType, int SourceId), double>();

    AddRanks(scores, semantic);
    AddRanks(scores, lexical);

    return scores
      .OrderByDescending(kv => kv.Value)
      .Select(kv => kv.Key)
      .ToList();
  }

  private static void AddRanks(
    Dictionary<(string SourceType, int SourceId), double> scores,
    IReadOnlyList<(string SourceType, int SourceId)> ranked)
  {
    for (var i = 0; i < ranked.Count; i++)
    {
      var key = ranked[i];
      var contribution = 1.0 / (RrfK + i + 1);
      scores[key] = scores.GetValueOrDefault(key) + contribution;
    }
  }

  private async Task<IReadOnlyList<int>> ResolveMatchIdsAsync(
    IReadOnlyList<(string SourceType, int SourceId)> fused,
    CancellationToken cancellationToken)
  {
    var analysisSourceIds = fused
      .Where(h => h.SourceType == DocumentChunkSourceType.MatchAnalysis)
      .Select(h => h.SourceId)
      .Distinct()
      .ToList();

    Dictionary<int, int> analysisToMatchId = [];
    if (analysisSourceIds.Count > 0)
    {
      analysisToMatchId = await db.MatchAnalysis
        .AsNoTracking()
        .Where(a => analysisSourceIds.Contains(a.Id))
        .ToDictionaryAsync(a => a.Id, a => a.MatchId, cancellationToken)
        .ConfigureAwait(false);
    }

    var rankedMatchIds = new List<int>(UniqueMatchCeiling);
    var seen = new HashSet<int>();

    foreach (var hit in fused)
    {
      int? matchId = hit.SourceType switch
      {
        DocumentChunkSourceType.Match => hit.SourceId,
        DocumentChunkSourceType.MatchAnalysis =>
          analysisToMatchId.TryGetValue(hit.SourceId, out var id) ? id : null,
        _ => null,
      };

      if (matchId is null || !seen.Add(matchId.Value))
        continue;

      rankedMatchIds.Add(matchId.Value);
      if (rankedMatchIds.Count >= UniqueMatchCeiling)
        break;
    }

    return rankedMatchIds;
  }
}
