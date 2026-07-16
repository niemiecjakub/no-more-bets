namespace NoMoreBets.Application.Common;

public interface IEmbeddingService
{
  Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}
