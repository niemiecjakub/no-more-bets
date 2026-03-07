using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NoMoreBets.Infrastructure.Scraping.External.SoccerData;
using NoMoreBets.Infrastructure.Scraping.External.SoccerData.Exceptions;
using NoMoreBets.Infrastructure.Tests.Helpers;
using Polly;
using Polly.Retry;

namespace NoMoreBets.Infrastructure.Tests.Scraping.External.SoccerData;

public class SoccerDataClientTests
{
  private static SoccerDataClient CreateClient(
      HttpClient? httpClient = null,
      IOptions<SoccerDataOptions>? options = null,
      ILogger<SoccerDataClient>? logger = null)
  {
    var client = httpClient ?? new HttpClient();
    var opts = options ?? Options.Create(new SoccerDataOptions { ApiKey = "test-key" });
    logger ??= NullLogger<SoccerDataClient>.Instance;
    return new SoccerDataClient(client, opts, logger);
  }

  [Fact]
  public async Task GetMatchPreviewsUpcomingAsync_WhenApiKeyMissing_ThrowsSoccerDataAuthException()
  {
    // Arrange: mock 401 so we don't depend on real API; client throws SoccerDataAuthException on 401
    var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
    var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.soccerdataapi.com/") };
    var options = Options.Create(new SoccerDataOptions { ApiKey = null });
    var sut = CreateClient(httpClient, options: options);

    // Act
    var act = () => sut.GetMatchPreviewsUpcomingAsync(null);

    // Assert
    await act.Should().ThrowAsync<SoccerDataAuthException>()
        .WithMessage("*Authentication*");
  }

  [Fact]
  public async Task GetMatchPreviewAsync_WhenHttpReturnsOk_ReturnsDeserializedPreview()
  {
    // Arrange
    var json = FixtureHelper.LoadFixtureText("soccerdata/match_preview.json");
    json.Should().NotBeNullOrEmpty();

    var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(json!, Encoding.UTF8, "application/json")
    });
    var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.soccerdataapi.com/") };
    var sut = CreateClient(httpClient);

    // Act
    var result = await sut.GetMatchPreviewAsync(955509);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(955509);
    result.Teams.Home.Name.Should().Be("Club Brugge");
    result.Teams.Away.Name.Should().Be("RAAL La Louviere");
  }

  [Fact]
  public async Task GetHeadToHeadAsync_WhenHttpReturnsOk_ReturnsDeserializedHeadToHead()
  {
    // Arrange
    var json = FixtureHelper.LoadFixtureText("soccerdata/head_to_head.json");
    json.Should().NotBeNullOrEmpty();

    var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(json!, Encoding.UTF8, "application/json")
    });
    var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.soccerdataapi.com/") };
    var sut = CreateClient(httpClient);

    // Act
    var result = await sut.GetHeadToHeadAsync(2916, 4148);

    // Assert
    result.Should().NotBeNull();
    result.Team1.Id.Should().Be(2916);
    result.Team2.Id.Should().Be(4148);
    result.Team1.Name.Should().Be("Chelsea");
    result.Team2.Name.Should().Be("Brentford");
  }

  [Fact]
  public async Task GetMatchPreviewAsync_WhenHttpReturns401_ThrowsSoccerDataAuthException()
  {
    // Arrange
    var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
    var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.soccerdataapi.com/") };
    var sut = CreateClient(httpClient);

    // Act
    var act = () => sut.GetMatchPreviewAsync(123);

    // Assert
    await act.Should().ThrowAsync<SoccerDataAuthException>()
        .WithMessage("*Authentication failed*");
  }

  [Fact]
  public async Task GetMatchPreviewAsync_WhenHttpReturns404_ThrowsSoccerDataNotFoundException()
  {
    // Arrange
    var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
    var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.soccerdataapi.com/") };
    var sut = CreateClient(httpClient);

    // Act
    var act = () => sut.GetMatchPreviewAsync(999999);

    // Assert
    await act.Should().ThrowAsync<SoccerDataNotFoundException>()
        .WithMessage("*Endpoint not found*");
  }

  [Fact]
  public async Task GetMatchPreviewsUpcomingAsync_WhenFilterByLeagueId_ReturnsOnlyMatchingLeague()
  {
    // Arrange: fixture is a root array; API returns { "results": array }
    var fixtureJson = FixtureHelper.LoadFixtureText("soccerdata/match_previews_upcoming.json");
    fixtureJson.Should().NotBeNullOrEmpty();
    var wrapped = "{\"results\": " + fixtureJson + "}";

    var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(wrapped, Encoding.UTF8, "application/json")
    });
    var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.soccerdataapi.com/") };
    var sut = CreateClient(httpClient);

    // Act: filter by league 212 (First Division A in fixture)
    var result = await sut.GetMatchPreviewsUpcomingAsync(212);

    // Assert
    result.Should().NotBeEmpty();
    result.Should().OnlyContain(r => r.LeagueId == 212);
  }

  [Fact]
  public async Task GetHeadToHeadAsync_WhenFirstAttemptFails_RetriesAndSucceeds()
  {
    // Arrange: client has no internal retry; Polly on the HttpClient performs retries
    var json = FixtureHelper.LoadFixtureText("soccerdata/head_to_head.json");
    json.Should().NotBeNullOrEmpty();

    var callCount = 0;
    var innerHandler = new MockHttpMessageHandler(_ =>
    {
      callCount++;
      if (callCount == 1)
        throw new HttpRequestException("Connection failed");
      return new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent(json!, Encoding.UTF8, "application/json")
      };
    });

    var retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
      .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
      {
        MaxRetryAttempts = 1,
        Delay = TimeSpan.FromMilliseconds(10),
        BackoffType = DelayBackoffType.Constant,
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>().Handle<HttpRequestException>()
      })
      .Build();
#pragma warning disable EXTEXP0001
    var resilienceHandler = new ResilienceHandler(retryPipeline) { InnerHandler = innerHandler };
#pragma warning restore EXTEXP0001

    var options = Options.Create(new SoccerDataOptions { ApiKey = "test-key" });
    var httpClient = new HttpClient(resilienceHandler) { BaseAddress = new Uri("https://api.soccerdataapi.com/") };
    var sut = CreateClient(httpClient, options: options);

    // Act
    var result = await sut.GetHeadToHeadAsync(2916, 4148);

    // Assert
    result.Should().NotBeNull();
    result.Team1.Id.Should().Be(2916);
    result.Team2.Id.Should().Be(4148);
    callCount.Should().Be(2);
  }

  [Fact]
  public async Task GetMatchesAsync_WhenResponseIsRootArray_DeserializesToLeagueMatchesList()
  {
    // Arrange: /matches/ returns root array
    var matchesJson = """
            [
              {
                "league_id": 39,
                "league_name": "Premier League",
                "country": { "id": 42, "name": "England" },
                "is_cup": false,
                "season": { "is_active": true, "year": "2024-2025" },
                "stage": []
              }
            ]
            """;

    var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(matchesJson, Encoding.UTF8, "application/json")
    });
    var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.soccerdataapi.com/") };
    var sut = CreateClient(httpClient);

    // Act
    var result = await sut.GetMatchesAsync(leagueId: 39);

    // Assert
    result.Should().HaveCount(1);
    result[0].LeagueId.Should().Be(39);
    result[0].LeagueName.Should().Be("Premier League");
  }

  [Fact]
  public async Task GetMatchesAsync_WithMatchesLeagueFixture_DeserializesSuccessfully()
  {
    // Arrange
    var json = FixtureHelper.LoadFixtureText("soccerdata/matches_league.json");
    json.Should().NotBeNullOrEmpty();

    var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(json!, Encoding.UTF8, "application/json")
    });
    var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.soccerdataapi.com/") };
    var sut = CreateClient(httpClient);

    // Act
    var result = await sut.GetMatchesAsync(leagueId: 228, season: "2025-2026");

    // Assert
    result.Should().NotBeEmpty();
    result[0].LeagueId.Should().Be(228);
    result[0].Stage.Should().NotBeEmpty();
    result[0].Stage[0].Matches.Should().NotBeEmpty();
  }

  [Fact]
  public async Task GetMatchesAsync_WhenFixtureContainsStringHandicapMarket_ParsesToDouble()
  {
    // Arrange: matches_league.json has "market": "+0.0" (string) in one match
    var json = FixtureHelper.LoadFixtureText("soccerdata/matches_league.json");
    json.Should().NotBeNullOrEmpty();

    var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(json!, Encoding.UTF8, "application/json")
    });
    var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.soccerdataapi.com/") };
    var sut = CreateClient(httpClient);

    // Act
    var result = await sut.GetMatchesAsync(leagueId: 228, season: "2025-2026");

    // Assert: fixture has "market": "+0.0" (string) in one match — parsed to 0.0
    var matchWithHandicapZero = result
      .SelectMany(l => l.Stage)
      .SelectMany(s => s.Matches)
      .FirstOrDefault(m => m.Odds?.Handicap?.Market == 0.0);
    matchWithHandicapZero.Should().NotBeNull("fixture contains a match with handicap market \"+0.0\" parsed to 0.0");
  }

    /// <summary>Handler that returns a configured response for testing.</summary>
  private sealed class MockHttpMessageHandler : HttpMessageHandler
  {
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
      _respond = respond;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      return Task.FromResult(_respond(request));
    }
  }

  [Fact]
  public async Task GetMatchPreviewAsync_WhenServerReturns500_ThrowsSoccerDataException()
  {
    // Arrange
    var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
    var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.soccerdataapi.com/") };
    var sut = CreateClient(httpClient);

    // Act
    var act = () => sut.GetMatchPreviewAsync(123);

    // Assert: client wraps failure in SoccerDataException
    await act.Should().ThrowAsync<SoccerDataException>();
  }

  [Fact]
  public async Task GetMatchPreviewAsync_WhenResponseIsInvalidJson_ThrowsSoccerDataException()
  {
    // Arrange
    var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent("not valid json {", Encoding.UTF8, "application/json")
    });
    var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.soccerdataapi.com/") };
    var sut = CreateClient(httpClient);

    // Act
    var act = () => sut.GetMatchPreviewAsync(123);

    // Assert: client wraps JsonException in SoccerDataException
    await act.Should().ThrowAsync<SoccerDataException>();
  }
}
