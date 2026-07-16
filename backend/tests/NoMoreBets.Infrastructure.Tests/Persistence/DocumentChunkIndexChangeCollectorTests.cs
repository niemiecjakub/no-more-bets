using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Infrastructure.Tests.Persistence;

public class DocumentChunkIndexChangeCollectorTests
{
  [Fact]
  public void AddFromEntry_MatchAdded_CollectsMatchSource()
  {
    // Arrange
    var pending = new List<(string SourceType, object Entity)>();
    var match = new Match { Id = 10 };

    // Act
    DocumentChunkIndexChangeCollector.AddFromEntry(pending, match, EntityState.Added);

    // Assert
    pending.Should().ContainSingle()
      .Which.Should().Be((DocumentChunkSourceType.Match, match));
  }

  [Fact]
  public void AddFromEntry_MatchAnalysisModified_CollectsMatchAnalysisSource()
  {
    // Arrange
    var pending = new List<(string SourceType, object Entity)>();
    var analysis = new MatchAnalysis { Id = 5, MatchId = 10, Code = "Research", Content = "{}" };

    // Act
    DocumentChunkIndexChangeCollector.AddFromEntry(pending, analysis, EntityState.Modified);

    // Assert
    pending.Should().ContainSingle()
      .Which.Should().Be((DocumentChunkSourceType.MatchAnalysis, analysis));
  }

  [Fact]
  public void AddFromEntry_LineupAdded_CollectsMatchSource()
  {
    // Arrange
    var pending = new List<(string SourceType, object Entity)>();
    var lineup = new Lineup { MatchId = 42, HomeTeamJson = "{}", AwayTeamJson = "{}" };

    // Act
    DocumentChunkIndexChangeCollector.AddFromEntry(pending, lineup, EntityState.Added);

    // Assert
    pending.Should().ContainSingle()
      .Which.SourceType.Should().Be(DocumentChunkSourceType.Match);
  }

  [Fact]
  public void AddFromEntry_MatchEventModified_CollectsMatchSource()
  {
    // Arrange
    var pending = new List<(string SourceType, object Entity)>();
    var matchEvent = new MatchEvent { Id = 1, MatchId = 7 };

    // Act
    DocumentChunkIndexChangeCollector.AddFromEntry(pending, matchEvent, EntityState.Modified);

    // Assert
    pending.Should().ContainSingle()
      .Which.SourceType.Should().Be(DocumentChunkSourceType.Match);
  }

  [Fact]
  public void AddFromEntry_Unchanged_Ignores()
  {
    // Arrange
    var pending = new List<(string SourceType, object Entity)>();

    // Act
    DocumentChunkIndexChangeCollector.AddFromEntry(pending, new Match { Id = 1 }, EntityState.Unchanged);

    // Assert
    pending.Should().BeEmpty();
  }

  [Fact]
  public void ResolveIds_DedupesAndMapsRelatedEntitiesToMatchId()
  {
    // Arrange
    var match = new Match { Id = 10 };
    var lineup = new Lineup { MatchId = 10, HomeTeamJson = "{}", AwayTeamJson = "{}" };
    var analysis = new MatchAnalysis { Id = 3, MatchId = 10, Code = "Research", Content = "{}" };
    var pending = new List<(string SourceType, object Entity)>
    {
      (DocumentChunkSourceType.Match, match),
      (DocumentChunkSourceType.Match, lineup),
      (DocumentChunkSourceType.MatchAnalysis, analysis)
    };

    // Act
    var resolved = DocumentChunkIndexChangeCollector.ResolveIds(pending);

    // Assert
    resolved.Should().BeEquivalentTo(
    [
      (DocumentChunkSourceType.Match, 10),
      (DocumentChunkSourceType.MatchAnalysis, 3)
    ]);
  }

  [Fact]
  public void ResolveIds_SkipsZeroIds()
  {
    // Arrange
    var pending = new List<(string SourceType, object Entity)>
    {
      (DocumentChunkSourceType.Match, new Match { Id = 0 })
    };

    // Act
    var resolved = DocumentChunkIndexChangeCollector.ResolveIds(pending);

    // Assert
    resolved.Should().BeEmpty();
  }
}
