using Pgvector;

namespace NoMoreBets.Domain.Matches;

public class DocumentChunk
{
  public int Id { get; set; }
  public string SourceType { get; set; } = null!;
  public int SourceId { get; set; }
  public int ChunkIndex { get; set; }
  public string Content { get; set; } = null!;
  public string? MetadataJson { get; set; }
  public Vector Embedding { get; set; } = null!;
  public string EmbeddingModel { get; set; } = null!;
  public DateTime UpdatedAt { get; set; }

  public DocumentChunkMetadata? GetMetadata() =>
    DocumentChunkMetadataJson.Deserialize(MetadataJson);

  public void SetMetadata(DocumentChunkMetadata metadata) =>
    MetadataJson = DocumentChunkMetadataJson.Serialize(metadata);
}
