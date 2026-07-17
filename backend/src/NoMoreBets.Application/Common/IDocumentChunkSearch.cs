namespace NoMoreBets.Application.Common;

public interface IDocumentChunkSearch
{
  /// <summary>
  /// Hybrid search: semantic (embedding) + lexical (full-text) fused with RRF.
  /// Returns unique match ids ranked by fused score.
  /// </summary>
  Task<IReadOnlyList<int>> FindMatchIdsAsync(
    string query,
    float[] embedding,
    string embeddingModelId,
    CancellationToken cancellationToken = default);
}
