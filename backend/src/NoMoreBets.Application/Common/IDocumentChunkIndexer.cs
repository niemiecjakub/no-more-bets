using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Common;

public interface IDocumentChunkIndexer
{
  Task IndexAsync(
    string sourceType,
    int sourceId,
    IDocumentChunkSource source,
    CancellationToken cancellationToken = default);
}
