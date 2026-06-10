using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NoMoreBets.Application.Common.Dto.Betting;
using NoMoreBets.Infrastructure.Scraping.BrowserAutomation;
using NoMoreBets.Infrastructure.Scraping.External.Betclic;
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Infrastructure.Tests.Helpers;

namespace NoMoreBets.Infrastructure.Tests.Scraping.External.Betclic;

public class BetclicScraperTests
{
  private const string PremierLeagueUrl = "https://www.betclic.pl/football-sfootball/premier-league-c3";

  private static BetclicScraper CreateScraper(
      PlaywrightPageFetcher? pageFetcher = null,
      BaseScraperOptions? baseOptions = null,
      BetclicScraperOptions? betclicOptions = null)
  {
    pageFetcher ??= PlaywrightPageFetcherMockHelper.CreateMock();
    var baseOpts = Options.Create(baseOptions ?? new BaseScraperOptions
    {
      DelaySeconds = 0,
      RetryCount = 3,
      RetryDelaySeconds = 0.01,
      TimeoutSeconds = 15
    });
    var betclicOpts = Options.Create(betclicOptions ?? new BetclicScraperOptions
    {
      EmptyResultRetryCount = 1,
      EmptyResultRetryDelayMinSeconds = 0,
      EmptyResultRetryDelayMaxSeconds = 0,
      MatchEventsRetryDelayMinSeconds = 0,
      MatchEventsRetryDelayMaxSeconds = 0
    });
    var logger = NullLogger<BetclicScraper>.Instance;
    return new BetclicScraper(pageFetcher, baseOpts, betclicOpts, logger);
  }

  private static string MinimalUpcomingGamesHtml()
  {
    return """
            <html><body>
            <div class="groupEvents">
                <h2 class="groupEvents_headTitle">Sob. 17/01</h2>
                <sports-events-event-card class="groupEvents_card">
                    <a class="cardEvent" href="/football-sfootball/premier-league-c3/arsenal-chelsea-m123"></a>
                    <div data-qa="contestant-1-label">Arsenal</div>
                    <div data-qa="contestant-2-label">Chelsea</div>
                    <div class="scoreboard_hour">13:30</div>
                    <div class="market_odds">
                        <button class="btn"><span class="btn_label is-top">1</span><span class="btn_label">2,10</span></button>
                        <button class="btn"><span class="btn_label is-top">X</span><span class="btn_label">3,40</span></button>
                        <button class="btn"><span class="btn_label is-top">2</span><span class="btn_label">3,20</span></button>
                    </div>
                </sports-events-event-card>
            </div>
            </body></html>
            """;
  }

  [Fact]
  public async Task ParseUpcomingGamesAsync_WithMinimalFixture_ParsesOneGame()
  {
    // Arrange
    var html = MinimalUpcomingGamesHtml();
    var sut = CreateScraper();

    // Act
    var result = await sut.ParseUpcomingGamesAsync(html);

    // Assert
    result.Should().HaveCount(1);
    result[0].Should().BeOfType<UpcomingGame>();
    result[0].HomeTeam.Should().Be("Arsenal");
    result[0].AwayTeam.Should().Be("Chelsea");
    result[0].Url.Should().Contain("betclic.pl").And.Contain("arsenal-chelsea");
    result[0].Date.Should().Be(new DateTime(DateTime.Today.Year, 1, 17));
    result[0].Time.Should().Be("13:30");
  }

  [Fact]
  public async Task ParseUpcomingGamesAsync_WithAnchorCardMarkup_ParsesOneGame()
  {
    // Arrange: newer Betclic markup where the card is the anchor itself
    // (<a sports-events-event-card class="cardEvent groupEvents_card">), as seen on the World Cup listing
    var html = """
            <html><body>
            <div class="groupEvents">
                <h2 class="groupEvents_headTitle">Niedz. 14/06</h2>
                <a sports-events-event-card class="cardEvent groupEvents_card" href="/pilka-nozna-sfootball/ms-c1/meksyk-rpa-m969329474007040">
                    <div data-qa="contestant-1-label">Meksyk</div>
                    <div data-qa="contestant-2-label">RPA</div>
                    <div class="scoreboard_hour">21:00</div>
                </a>
            </div>
            </body></html>
            """;
    var sut = CreateScraper();

    // Act
    var result = await sut.ParseUpcomingGamesAsync(html);

    // Assert
    result.Should().HaveCount(1);
    result[0].HomeTeam.Should().Be("Meksyk");
    result[0].AwayTeam.Should().Be("RPA");
    result[0].Time.Should().Be("21:00");
    result[0].Url.Should().Contain("betclic.pl").And.Contain("meksyk-rpa");
    result[0].Date.Should().Be(new DateTime(DateTime.Today.Year, 6, 14));
  }

  [Fact]
  public async Task ParseUpcomingGamesAsync_WithNoGroupEvents_ReturnsEmpty()
  {
    // Arrange
    var html = "<html><body></body></html>";
    var sut = CreateScraper();

    // Act
    var result = await sut.ParseUpcomingGamesAsync(html);

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public async Task ParseUpcomingGamesAsync_WhenContestantLabelsMissing_AddsCardWithEmptyTeamNames()
  {
    // Arrange: card has no data-qa contestant labels so home/away become ""
    var html = """
            <html><body>
            <div class="groupEvents">
                <h2 class="groupEvents_headTitle">Sob. 17/01</h2>
                <sports-events-event-card class="groupEvents_card">
                    <a class="cardEvent" href="/football-sfootball/premier-league-c3/unknown-m123"></a>
                    <div class="scoreboard_hour">14:00</div>
                </sports-events-event-card>
            </div>
            </body></html>
            """;
    var sut = CreateScraper();

    // Act
    var result = await sut.ParseUpcomingGamesAsync(html);

    // Assert: card is still added but with empty team names (scraper does not skip)
    result.Should().HaveCount(1);
    result[0].HomeTeam.Should().BeEmpty();
    result[0].AwayTeam.Should().BeEmpty();
    result[0].Time.Should().Be("14:00");
  }

  [Fact]
  public async Task GetUpcomingGamesAsync_WhenFetcherReturnsMinimalFixture_ParsesGames()
  {
    // Arrange
    var html = MinimalUpcomingGamesHtml();
    var pageFetcher = PlaywrightPageFetcherMockHelper.CreateMock();
    pageFetcher
        .GetHtmlAfterInteractionsAsync(
            PremierLeagueUrl,
            Arg.Any<IReadOnlyList<InteractionStep>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<bool>())
        .Returns(Task.FromResult(html));
    var sut = CreateScraper(pageFetcher);

    // Act
    var result = await sut.GetUpcomingGamesAsync("premier-league");

    // Assert
    result.Should().NotBeEmpty();
    result[0].HomeTeam.Should().Be("Arsenal");
    result[0].AwayTeam.Should().Be("Chelsea");
    result[0].Url.Should().NotBeNullOrEmpty();
    await pageFetcher.Received(1)
        .GetHtmlAfterInteractionsAsync(
            PremierLeagueUrl,
            Arg.Any<IReadOnlyList<InteractionStep>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<bool>());
  }

  [Theory]
  [InlineData("premier-league", "https://www.betclic.pl/football-sfootball/premier-league-c3")]
  [InlineData("ekstraklasa", "https://www.betclic.pl/football-sfootball/ekstraklasa-c221")]
  [InlineData("laliga", "https://www.betclic.pl/football-sfootball/la-liga-c7")]
  [InlineData("bundesliga", "https://www.betclic.pl/football-sfootball/bundesliga-c5")]
  [InlineData("serie-a", "https://www.betclic.pl/football-sfootball/serie-a-c6")]
  [InlineData("ligue-1", "https://www.betclic.pl/football-sfootball/ligue-1-c4")]
  [InlineData("fifa-world-cup", "https://www.betclic.pl/football-sfootball/ms-c1")]
  public async Task GetUpcomingGamesAsync_ForLeagueSlug_RequestsConfiguredListingUrl(string leagueSlug, string expectedListingUrl)
  {
    var html = MinimalUpcomingGamesHtml();
    var pageFetcher = PlaywrightPageFetcherMockHelper.CreateMock();
    pageFetcher
        .GetHtmlAfterInteractionsAsync(
            expectedListingUrl,
            Arg.Any<IReadOnlyList<InteractionStep>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<bool>())
        .Returns(Task.FromResult(html));
    var sut = CreateScraper(pageFetcher);

    await sut.GetUpcomingGamesAsync(leagueSlug);

    await pageFetcher.Received(1)
        .GetHtmlAfterInteractionsAsync(
            expectedListingUrl,
            Arg.Any<IReadOnlyList<InteractionStep>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<bool>());
  }

  [Fact]
  public async Task GetUpcomingGamesAsync_WhenLeagueSlugNotMapped_DoesNotFetchAndReturnsEmpty()
  {
    var pageFetcher = PlaywrightPageFetcherMockHelper.CreateMock();
    var sut = CreateScraper(pageFetcher);

    var result = await sut.GetUpcomingGamesAsync("not-a-league");

    result.Should().BeEmpty();
    await pageFetcher.DidNotReceive()
      .GetHtmlAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task ExtractEventsAsync_WithMatchFixture_ParsesEvents()
  {
    // Arrange
    var html = FixtureHelper.LoadFixtureText("betclic/match_page.html");
    html.Should().NotBeNull("fixture file must exist");
    var sut = CreateScraper();

    // Act
    var result = await sut.ExtractEventsAsync(html!);

    // Assert
    result.Should().NotBeEmpty();
    result.Should().OnlyContain(e => e is BookmakerEvent);
    result.Should().Contain(e => !string.IsNullOrEmpty(e.Title) && e.Options.Count > 0);
  }

  [Fact]
  public async Task GetMatchEventsAsync_WhenFetcherReturnsMatchFixture_ParsesEvents()
  {
    // Arrange
    var html = FixtureHelper.LoadFixtureText("betclic/match_page.html");
    html.Should().NotBeNull("fixture file must exist");
    var gameUrl = "https://www.betclic.pl/pilka-nozna-sfootball/premier-league-c3/bournemouth-liverpool-m905675307745280";
    var pageFetcher = PlaywrightPageFetcherMockHelper.CreateMock();
    pageFetcher.GetHtmlAsync(gameUrl, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(html!));
    var sut = CreateScraper(pageFetcher);

    // Act
    var result = await sut.GetMatchEventsAsync(gameUrl, expand: false);

    // Assert
    result.Should().NotBeEmpty();
    result.Should().OnlyContain(e => e is BookmakerEvent);
    result.Should().Contain(e => !string.IsNullOrEmpty(e.Title) && e.Options.Count > 0);
    await pageFetcher.Received(1).GetHtmlAsync(gameUrl, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetMatchEventsAsync_WhenExpandTrue_CallsInteractiveFetcherAndParsesEvents()
  {
    // Arrange
    var html = FixtureHelper.LoadFixtureText("betclic/match_page.html");
    html.Should().NotBeNull("fixture file must exist");
    var gameUrl = "https://www.betclic.pl/some-match";
    var pageFetcher = PlaywrightPageFetcherMockHelper.CreateMock();
    pageFetcher
        .GetHtmlAfterInteractionsAsync(
            gameUrl,
            Arg.Any<IReadOnlyList<InteractionStep>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<bool>())
        .Returns(Task.FromResult(html!));
    var sut = CreateScraper(pageFetcher);

    // Act
    var result = await sut.GetMatchEventsAsync(gameUrl, expand: true);

    // Assert
    result.Should().NotBeEmpty();
    await pageFetcher.Received(1)
        .GetHtmlAfterInteractionsAsync(
            gameUrl,
            Arg.Any<IReadOnlyList<InteractionStep>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<bool>());
  }
}
