using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NoMoreBets.Application.Search;
using NoMoreBets.Infrastructure.Search;

namespace NoMoreBets.Infrastructure.Tests.Http;

public class BraveSearchTests
{
  private static BraveSearch CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
  {
    var handler = new MockHandler(responder);
    var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.search.brave.com") };
    return new BraveSearch(httpClient, NullLogger<BraveSearch>.Instance);
  }

  [Fact]
  public async Task SearchAsync_WhenResponseContainsResults_MapsToDto()
  {
    var payload = JsonSerializer.Serialize(new
    {
      type = "search",
      query = new { original = "brave", country = "" },
      web = new
      {
        type = "search",
        results = new[]
        {
          new { type = "web_result", title = "Brave", url = "https://brave.com", description = "Brave browser" }
        }
      }
    });

    var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(payload, Encoding.UTF8, "application/json")
    });

    var result = await client.SearchAsync("brave", new SearchOptions());

    result.Items.Should().HaveCount(1);
    result.Items[0].Title.Should().Be("Brave");
    result.Items[0].Url.Should().Be("https://brave.com");
    result.Items[0].Snippet.Should().Be("Brave browser");
  }

  [Fact]
  public async Task SearchAsync_WhenResponseContainsMetaUrlAndThumbnail_MapsEnrichedFields()
  {
    var payload = """
    {
      "type": "search",
      "query": { "original": "premier league", "country": "gb" },
      "web": {
        "type": "search",
        "results": [
          {
            "type": "web_result",
            "title": "Premier League",
            "url": "https://example.com/pl",
            "description": "Top flight league.",
            "meta_url": {
              "scheme": "https",
              "netloc": "example.com",
              "hostname": "www.example.com",
              "favicon": "https://favicon.example.com/icon",
              "path": "› sport › premier-league"
            },
            "thumbnail": { "src": "https://thumb.example.com/pl.jpg" }
          }
        ]
      }
    }
    """;

    var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(payload, Encoding.UTF8, "application/json")
    });

    var result = await client.SearchAsync("premier league", new SearchOptions());

    result.Items.Should().HaveCount(1);
    result.Items[0].Hostname.Should().Be("www.example.com");
    result.Items[0].DisplayUrlPath.Should().Be("› sport › premier-league");
    result.Items[0].ThumbnailUrl.Should().Be("https://thumb.example.com/pl.jpg");
  }

  [Fact]
  public async Task SearchAsync_BuildsExpectedWebSearchUrl()
  {
    HttpRequestMessage? capturedRequest = null;
    var payload = """{"type":"search","query":{},"web":{"type":"search","results":[]}}""";
    var client = CreateClient(req =>
    {
      capturedRequest = req;
      return new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent(payload, Encoding.UTF8, "application/json")
      };
    });

    await client.SearchAsync("premier league", new SearchOptions());

    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.AbsolutePath.Should().Be("/res/v1/web/search");
    capturedRequest.RequestUri!.Query.Should().Contain("q=").And.Contain("premier");
  }

  [Fact]
  public async Task SearchAsync_WhenEmptyQuery_ThrowsArgumentException()
  {
    var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

    var act = () => client.SearchAsync("", new SearchOptions());

    await act.Should().ThrowAsync<ArgumentException>().WithParameterName("q");
  }

  [Fact]
  public async Task SearchAsync_WhenWebResultsEmpty_ReturnsEmptyItems()
  {
    var payload = """{"type":"search","query":{},"web":{"type":"search","results":[]}}""";
    var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(payload, Encoding.UTF8, "application/json")
    });

    var result = await client.SearchAsync("test", new SearchOptions());

    result.Items.Should().BeEmpty();
  }

  [Fact]
  public async Task SearchNewsAsync_WhenResponseContainsResults_MapsToDto()
  {
    var payload = """
    {
      "type": "news",
      "query": { "original": "premier league", "country": "gb" },
      "results": [
        {
          "type": "news_result",
          "title": "Chelsea handed suspended transfer ban",
          "url": "https://example.com/chelsea",
          "description": "Chelsea have been handed a suspended ban.",
          "age": "1 day ago",
          "page_age": "2026-03-16T13:03:21",
          "meta_url": {
            "scheme": "https",
            "netloc": "example.com",
            "hostname": "www.example.com",
            "favicon": "",
            "path": ""
          },
          "thumbnail": { "src": "https://thumb.example.com/c.jpg" },
          "extra_snippets": ["First snippet.", "Second snippet."]
        }
      ]
    }
    """;

    var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(payload, Encoding.UTF8, "application/json")
    });

    var result = await client.SearchNewsAsync("premier league", new SearchNewsOptions());

    result.Items.Should().HaveCount(1);
    result.Items[0].Title.Should().Be("Chelsea handed suspended transfer ban");
    result.Items[0].Url.Should().Be("https://example.com/chelsea");
    result.Items[0].Snippet.Should().Be("Chelsea have been handed a suspended ban.");
    result.Items[0].Source.Should().Be("www.example.com");
    result.Items[0].Age.Should().Be("1 day ago");
    result.Items[0].Hostname.Should().Be("www.example.com");
    result.Items[0].ThumbnailUrl.Should().Be("https://thumb.example.com/c.jpg");
    result.Items[0].ExtraSnippets.Should().Equal("First snippet.", "Second snippet.");
  }

  [Fact]
  public async Task SearchNewsAsync_BuildsExpectedNewsUrl()
  {
    HttpRequestMessage? capturedRequest = null;
    var payload = """{"type":"news","query":{},"results":[]}""";
    var client = CreateClient(req =>
    {
      capturedRequest = req;
      return new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent(payload, Encoding.UTF8, "application/json")
      };
    });

    await client.SearchNewsAsync("machine learning", new SearchNewsOptions { Freshness = "week", Country = "US" });

    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.AbsolutePath.Should().Be("/res/v1/news/search");
    capturedRequest.RequestUri!.Query.Should().Contain("q=").And.Contain("machine");
    capturedRequest.RequestUri!.Query.Should().Contain("freshness=week");
    capturedRequest.RequestUri!.Query.Should().Contain("country=US");
  }

  [Fact]
  public async Task SearchNewsAsync_WhenEmptyQuery_ThrowsArgumentException()
  {
    var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

    var act = () => client.SearchNewsAsync("   ", new SearchNewsOptions());

    await act.Should().ThrowAsync<ArgumentException>().WithParameterName("q");
  }

  [Fact]
  public async Task SearchLlmContextAsync_WhenResponseContainsResults_MapsToDto()
  {
    var payload = """
    {
      "type": "llm_context",
      "results": [
        {
          "content": "The Premier League is a professional football league.",
          "url": "https://en.wikipedia.org/wiki/Premier_League",
          "tokens": 42,
          "title": "Premier League",
          "score": 0.95,
          "source_type": "generic"
        }
      ]
    }
    """;

    var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(payload, Encoding.UTF8, "application/json")
    });

    var result = await client.SearchLlmContextAsync("premier league", new SearchLlmContextOptions());

    result.Items.Should().HaveCount(1);
    result.Items[0].Text.Should().Contain("Premier League");
    result.Items[0].Url.Should().Be("https://en.wikipedia.org/wiki/Premier_League");
    result.Items[0].TokenCount.Should().Be(42);
    result.Items[0].Title.Should().Be("Premier League");
    result.Items[0].Score.Should().Be(0.95);
    result.Items[0].SourceType.Should().Be("generic");
  }

  [Fact]
  public async Task SearchLlmContextAsync_BuildsExpectedLlmContextUrl()
  {
    HttpRequestMessage? capturedRequest = null;
    var payload = """{"type":"llm_context","results":[]}""";
    var client = CreateClient(req =>
    {
      capturedRequest = req;
      return new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent(payload, Encoding.UTF8, "application/json")
      };
    });

    await client.SearchLlmContextAsync("how deep is the mediterranean sea", new SearchLlmContextOptions
    {
      MaximumNumberOfTokens = 2048,
      Count = 10
    });

    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.AbsolutePath.Should().Be("/res/v1/llm/context");
    capturedRequest.RequestUri!.Query.Should().Contain("q=").And.Contain("mediterranean");
    capturedRequest.RequestUri!.Query.Should().Contain("maximum_number_of_tokens=2048");
    capturedRequest.RequestUri!.Query.Should().Contain("count=10");
  }

  [Fact]
  public async Task SearchLlmContextAsync_WhenEmptyQuery_ThrowsArgumentException()
  {
    var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

    var act = () => client.SearchLlmContextAsync("", new SearchLlmContextOptions());

    await act.Should().ThrowAsync<ArgumentException>().WithParameterName("q");
  }

  [Fact]
  public async Task SearchAsync_WhenServerReturns500_ThrowsBraveSearchException()
  {
    var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

    var act = () => client.SearchAsync("test", new SearchOptions());

    await act.Should().ThrowAsync<BraveSearchException>();
  }

  [Fact]
  public async Task SearchAsync_WhenResponseIsInvalidJson_ThrowsJsonException()
  {
    var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent("not valid json {", Encoding.UTF8, "application/json")
    });

    var act = () => client.SearchAsync("test", new SearchOptions());

    await act.Should().ThrowAsync<JsonException>();
  }

  private sealed class MockHandler : HttpMessageHandler
  {
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public MockHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
      _responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      return Task.FromResult(_responder(request));
    }
  }
}

