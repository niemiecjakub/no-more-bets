using System.Collections.Concurrent;
using System.Text.Json;
using NoMoreBets.Infrastructure.AI.Providers.WebSearch;

namespace NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;

public sealed class AgentRunToolMetadataCollector
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  private readonly ConcurrentDictionary<string, string> _byCallId = new(StringComparer.Ordinal);

  public void Reset() => _byCallId.Clear();

  public void Record(string? callId, IReadOnlyList<WebSearchToolSourceMetadata> sources)
  {
    if (sources.Count == 0 || string.IsNullOrEmpty(callId))
      return;

    var metadataJson = JsonSerializer.Serialize(new ToolMetadataPayload(sources), JsonOptions);
    _byCallId[callId] = metadataJson;
  }

  public string? TryTake(string? callId)
  {
    if (string.IsNullOrEmpty(callId))
      return null;

    return _byCallId.TryRemove(callId, out var metadataJson) ? metadataJson : null;
  }

  private sealed record ToolMetadataPayload(IReadOnlyList<WebSearchToolSourceMetadata> Sources);
}
