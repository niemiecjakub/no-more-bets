using FluentAssertions;
using NoMoreBets.Infrastructure.AI.Common.Embedding;

namespace NoMoreBets.Infrastructure.Tests.AI.Common.Embedding;

public class DocumentChunkIndexerTests
{
  [Fact]
  public void SplitChunks_TextUnderMaxLength_ReturnsSingleChunk()
  {
    // Arrange
    var text = "short text";

    // Act
    var chunks = DocumentChunkIndexer.SplitChunks(text, maxLength: 100);

    // Assert
    chunks.Should().ContainSingle().Which.Should().Be(text);
  }

  [Fact]
  public void SplitChunks_TextOverMaxLength_SplitsOnParagraphs()
  {
    // Arrange
    var text = "first paragraph\n\nsecond paragraph that is longer";

    // Act
    var chunks = DocumentChunkIndexer.SplitChunks(text, maxLength: 20, overlap: 2);

    // Assert
    chunks.Should().HaveCountGreaterThan(1);
    chunks.Should().OnlyContain(chunk => chunk.Length <= 20);
    chunks[0][^2..].Should().Be(chunks[1][..2]);
  }

  [Fact]
  public void SplitChunks_LongParagraph_OverlapsAdjacentChunks()
  {
    // Arrange
    var text = "abcdefghij";

    // Act
    var chunks = DocumentChunkIndexer.SplitChunks(text, maxLength: 6, overlap: 2);

    // Assert
    chunks.Should().Equal("abcdef", "efghij");
  }
}
