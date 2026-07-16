using System.Text.Json;

namespace NoMoreBets.Domain.Matches;

public sealed record DocumentChunkMetadata(
  IReadOnlyList<int> ClubIds,
  int? LeagueId);

public static class DocumentChunkMetadataJson
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public static string Serialize(DocumentChunkMetadata metadata) =>
    JsonSerializer.Serialize(metadata, JsonOptions);

  public static DocumentChunkMetadata? Deserialize(string? json) =>
    string.IsNullOrWhiteSpace(json)
      ? null
      : JsonSerializer.Deserialize<DocumentChunkMetadata>(json, JsonOptions);
}
