using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.MatchSearch;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Infrastructure.MatchSearch;

public sealed class DocumentChunkIndexer(
  AppDbContext db,
  IEmbeddingService embeddingService,
  ILogger<DocumentChunkIndexer> logger) : IDocumentChunkIndexer
{
  public const int DefaultChunkMaxLength = 1500;
  public const int DefaultChunkOverlap = 150;

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
}
