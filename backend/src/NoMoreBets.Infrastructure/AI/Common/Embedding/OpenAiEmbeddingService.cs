using System.ClientModel;
using Microsoft.Extensions.Options;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI;
using OpenAI;

namespace NoMoreBets.Infrastructure.AI.Common.Embedding;

public sealed class OpenAiEmbeddingService : IEmbeddingService
{
  private readonly OpenAIOptions _options;

  public OpenAiEmbeddingService(IOptions<OpenAIOptions> openAiOptions)
  {
    _options = openAiOptions.Value;
    ArgumentException.ThrowIfNullOrWhiteSpace(_options.EmbeddingModelId, nameof(OpenAIOptions.EmbeddingModelId));
  }

  public string ModelId => _options.EmbeddingModelId;

  public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(text))
      throw new ArgumentException("Text to embed must not be empty.", nameof(text));

    var client = new OpenAIClient(new ApiKeyCredential(_options.ApiKey)).GetEmbeddingClient(ModelId);
    var result = await client
      .GenerateEmbeddingAsync(text, cancellationToken: cancellationToken)
      .ConfigureAwait(false);

    return result.Value.ToFloats().ToArray();
  }
}
