namespace NoMoreBets.Application.Common;

public interface IEmbeddingService
{
  string ModelId { get; }

  Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}
