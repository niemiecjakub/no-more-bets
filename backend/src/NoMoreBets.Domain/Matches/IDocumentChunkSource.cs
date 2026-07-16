namespace NoMoreBets.Domain.Matches;

public interface IDocumentChunkSource
{
  string? BuildEmbeddingText();

  DocumentChunkMetadata BuildMetadata();
}
