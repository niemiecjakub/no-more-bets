using System.Text.Json;
using FluentAssertions;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using NoMoreBets.Infrastructure.AI.Providers.WebSearch;

namespace NoMoreBets.Infrastructure.Tests.AI.Middlewares.AgentResponseMapping;

public class AgentRunToolMetadataCollectorTests
{
  [Fact]
  public void Record_ReturnsSourcesArray()
  {
    var collector = new AgentRunToolMetadataCollector();
    collector.Record(
      "call-news",
      [
        new WebSearchToolSourceMetadata("Title A", "https://a.example/news", "a.example"),
        new WebSearchToolSourceMetadata("Title B", "https://b.example/news", "b.example"),
      ]);

    var metadata = collector.TryTake("call-news");

    metadata.Should().NotBeNull();
    using var document = JsonDocument.Parse(metadata!);
    var sources = document.RootElement.GetProperty("sources");
    sources.GetArrayLength().Should().Be(2);
    sources[0].GetProperty("title").GetString().Should().Be("Title A");
    sources[0].GetProperty("url").GetString().Should().Be("https://a.example/news");
    sources[0].GetProperty("hostname").GetString().Should().Be("a.example");
  }

  [Fact]
  public void Record_SingleSource_ReturnsOneSource()
  {
    var collector = new AgentRunToolMetadataCollector();
    collector.Record(
      "call-grounding",
      [new WebSearchToolSourceMetadata("Grounding Title", "https://wiki.example/page", "wiki.example")]);

    var metadata = collector.TryTake("call-grounding");

    metadata.Should().NotBeNull();
    using var document = JsonDocument.Parse(metadata!);
    var sources = document.RootElement.GetProperty("sources");
    sources.GetArrayLength().Should().Be(1);
    sources[0].GetProperty("title").GetString().Should().Be("Grounding Title");
  }

  [Fact]
  public void TryTake_WrongCallId_ReturnsNullAndKeepsEntry()
  {
    var collector = new AgentRunToolMetadataCollector();
    collector.Record(
      "call-news",
      [new WebSearchToolSourceMetadata("Title", "https://example.com", "example.com")]);

    collector.TryTake("call-other").Should().BeNull();

    var metadata = collector.TryTake("call-news");
    metadata.Should().NotBeNull();
  }

  [Fact]
  public void TryTake_CanResolveCallsOutOfRecordOrder()
  {
    var collector = new AgentRunToolMetadataCollector();
    collector.Record(
      "call-news",
      [new WebSearchToolSourceMetadata("News", "https://news.example", "news.example")]);
    collector.Record(
      "call-grounding",
      [new WebSearchToolSourceMetadata("Grounding", "https://wiki.example", "wiki.example")]);

    collector.TryTake("call-grounding").Should().NotBeNull();
    collector.TryTake("call-news").Should().NotBeNull();
  }

  [Fact]
  public void TryTake_AfterDequeue_ReturnsNull()
  {
    var collector = new AgentRunToolMetadataCollector();
    collector.Record(
      "call-news",
      [new WebSearchToolSourceMetadata("Title", "https://example.com", "example.com")]);

    collector.TryTake("call-news").Should().NotBeNull();
    collector.TryTake("call-news").Should().BeNull();
  }

  [Fact]
  public void Reset_ClearsPendingEntries()
  {
    var collector = new AgentRunToolMetadataCollector();
    collector.Record(
      "call-news",
      [new WebSearchToolSourceMetadata("Title", "https://example.com", "example.com")]);

    collector.Reset();

    collector.TryTake("call-news").Should().BeNull();
  }
}
