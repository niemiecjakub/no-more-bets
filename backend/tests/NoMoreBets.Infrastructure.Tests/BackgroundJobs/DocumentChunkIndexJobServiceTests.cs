using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.BackgroundJobs;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Infrastructure.Tests.BackgroundJobs;

public class DocumentChunkIndexJobServiceTests
{
  private readonly IDocumentChunkSourceLoader _loader = Substitute.For<IDocumentChunkSourceLoader>();
  private readonly IDocumentChunkIndexer _indexer = Substitute.For<IDocumentChunkIndexer>();
  private readonly DocumentChunkIndexJobService _sut;

  public DocumentChunkIndexJobServiceTests()
  {
    _loader.LoadAsync(default!, default, default).ReturnsForAnyArgs((IDocumentChunkSource?)null);
    _sut = new DocumentChunkIndexJobService(_loader, _indexer, NullLogger<DocumentChunkIndexJobService>.Instance);
  }

  [Fact]
  public async Task IndexAsync_UnsupportedSourceType_DoesNotCallIndexer()
  {
    // Act
    await _sut.IndexAsync("Unknown", 1);

    // Assert
    await _indexer.DidNotReceiveWithAnyArgs().IndexAsync(default!, default, default!, default);
  }

  [Fact]
  public async Task IndexAsync_MissingSource_DoesNotCallIndexer()
  {
    // Act
    await _sut.IndexAsync(DocumentChunkSourceType.Match, 999);

    // Assert
    await _indexer.DidNotReceiveWithAnyArgs().IndexAsync(default!, default, default!, default);
  }

  [Fact]
  public async Task IndexAsync_ExistingSource_CallsIndexer()
  {
    // Arrange
    var source = Substitute.For<IDocumentChunkSource>();
    _loader.LoadAsync(DocumentChunkSourceType.Match, 42, Arg.Any<CancellationToken>())
      .Returns(source);

    // Act
    await _sut.IndexAsync(DocumentChunkSourceType.Match, 42);

    // Assert
    await _indexer.Received(1).IndexAsync(
      DocumentChunkSourceType.Match,
      42,
      source,
      Arg.Any<CancellationToken>());
  }
}
