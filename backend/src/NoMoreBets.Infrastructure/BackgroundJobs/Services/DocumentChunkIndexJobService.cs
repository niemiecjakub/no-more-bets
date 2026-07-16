using Hangfire;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class DocumentChunkIndexJobService(
  IDocumentChunkSourceLoader sourceLoader,
  IDocumentChunkIndexer indexer,
  ILogger<DocumentChunkIndexJobService> logger)
{
  [AutomaticRetry(Attempts = 3)]
  public async Task IndexAsync(string sourceType, int sourceId)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(sourceType);

    var source = await sourceLoader.LoadAsync(sourceType, sourceId).ConfigureAwait(false);
    if (source is null)
    {
      logger.LogWarning(
        "Skipping DocumentChunk index for {SourceType}/{SourceId}: source not found or unsupported",
        sourceType,
        sourceId);
      return;
    }

    await indexer.IndexAsync(sourceType, sourceId, source, CancellationToken.None).ConfigureAwait(false);
  }
}
