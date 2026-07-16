using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Persistence;
using Pgvector;

namespace NoMoreBets.Infrastructure.AI.Common.Embedding;

public sealed class DocumentChunkIndexer(
  AppDbContext db,
  IEmbeddingService embeddingService,
  ILogger<DocumentChunkIndexer> logger) : IDocumentChunkIndexer
{
  public const int DefaultChunkMaxLength = 1500;
  public const int DefaultChunkOverlap = 150;

  public async Task IndexAsync(
    string sourceType,
    int sourceId,
    IDocumentChunkSource source,
    CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(sourceType);
    ArgumentNullException.ThrowIfNull(source);

    var text = source.BuildEmbeddingText();
    if (string.IsNullOrWhiteSpace(text))
    {
      await DeleteSourceChunksAsync(sourceType, sourceId, cancellationToken).ConfigureAwait(false);
      await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
      logger.LogInformation(
        "Cleared DocumentChunk rows for {SourceType}/{SourceId} (no embedding text)",
        sourceType,
        sourceId);
      return;
    }

    var contents = SplitChunks(text);
    var metadata = source.BuildMetadata();
    var model = embeddingService.ModelId;
    var existing = await db.DocumentChunk
      .Where(c => c.SourceType == sourceType && c.SourceId == sourceId && c.EmbeddingModel == model)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    var existingByIndex = existing.ToDictionary(c => c.ChunkIndex);
    var keepIndexes = new HashSet<int>();

    for (var i = 0; i < contents.Count; i++)
    {
      var content = contents[i];
      keepIndexes.Add(i);

      if (existingByIndex.TryGetValue(i, out var row) && row.Content == content)
      {
        row.SetMetadata(metadata);
        row.UpdatedAt = DateTime.UtcNow;
        continue;
      }

      var embedding = await embeddingService.EmbedAsync(content, cancellationToken).ConfigureAwait(false);

      if (row is not null)
      {
        row.Content = content;
        row.Embedding = new Vector(embedding);
        row.SetMetadata(metadata);
        row.UpdatedAt = DateTime.UtcNow;
      }
      else
      {
        var chunk = new DocumentChunk
        {
          SourceType = sourceType,
          SourceId = sourceId,
          ChunkIndex = i,
          Content = content,
          Embedding = new Vector(embedding),
          EmbeddingModel = model,
          UpdatedAt = DateTime.UtcNow
        };
        chunk.SetMetadata(metadata);
        db.DocumentChunk.Add(chunk);
      }
    }

    foreach (var stale in existing.Where(c => !keepIndexes.Contains(c.ChunkIndex)))
      db.DocumentChunk.Remove(stale);

    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    logger.LogInformation(
      "Indexed {ChunkCount} DocumentChunk row(s) for {SourceType}/{SourceId}",
      contents.Count,
      sourceType,
      sourceId);
  }

  public static IReadOnlyList<string> SplitChunks(
    string text,
    int maxLength = DefaultChunkMaxLength,
    int? overlap = null)
  {
    if (string.IsNullOrEmpty(text))
      return [];

    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLength);
    if (overlap < 0 || overlap >= maxLength)
      throw new ArgumentOutOfRangeException(nameof(overlap), overlap, "Overlap must be smaller than max length.");

    if (text.Length <= maxLength)
      return [text];

    var chunkOverlap = overlap ?? Math.Min(DefaultChunkOverlap, maxLength / 10);
    var normalizedText = string.Join(
      "\n\n",
      text.Replace("\r\n", "\n", StringComparison.Ordinal)
        .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    var chunks = new List<string>();
    var start = 0;
    while (start < normalizedText.Length)
    {
      var end = Math.Min(start + maxLength, normalizedText.Length);
      if (end < normalizedText.Length)
      {
        var paragraphBoundary = normalizedText.LastIndexOf(
          "\n\n",
          end - 1,
          end - start,
          StringComparison.Ordinal);
        if (paragraphBoundary > start + chunkOverlap)
          end = paragraphBoundary;
      }

      chunks.Add(normalizedText[start..end].Trim());
      if (end == normalizedText.Length)
        break;

      start = end - chunkOverlap;
    }

    return chunks;
  }

  private async Task DeleteSourceChunksAsync(string sourceType, int sourceId, CancellationToken cancellationToken)
  {
    var rows = await db.DocumentChunk
      .Where(c => c.SourceType == sourceType && c.SourceId == sourceId)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    db.DocumentChunk.RemoveRange(rows);
  }
}
