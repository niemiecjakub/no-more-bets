using System.Text.Json;

namespace NoMoreBets.Domain.Matches;

public sealed record DocumentChunkMetadata(
  IReadOnlyList<int> ClubIds,
  int? LeagueId)
{
  public static Builder CreateBuilder() => new();

  public sealed class Builder
  {
    private readonly List<int> _clubIds = [];
    private int? _leagueId;

    public Builder WithClubIds(IEnumerable<int> clubIds)
    {
      _clubIds.AddRange(clubIds);
      return this;
    }

    public Builder WithLeagueId(int? leagueId)
    {
      _leagueId = leagueId;
      return this;
    }

    public DocumentChunkMetadata Build() =>
      new([.. _clubIds], _leagueId);
  }
}

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
