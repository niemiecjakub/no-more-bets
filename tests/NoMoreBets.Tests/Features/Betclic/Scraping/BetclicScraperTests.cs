using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NoMoreBets.Features.Betclic.Model;
using NoMoreBets.Features.Betclic.Scraping;
using NoMoreBets.Infrastructure.Fetching;
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Infrastructure.Storage;
using NoMoreBets.Tests.Helpers;

namespace NoMoreBets.Tests.Features.Betclic.Scraping;

public class BetclicScraperTests
{
    private const string PremierLeagueUrl = "https://www.betclic.pl/football-sfootball/premier-league-c3";

    private static BetclicScraper CreateScraper(
        IHtmlCache? cache = null,
        IPageFetcher? fetcher = null,
        IInteractivePageFetcher? interactiveFetcher = null,
        BaseScraperOptions? baseOptions = null,
        BetclicScraperOptions? betclicOptions = null)
    {
        cache ??= Substitute.For<IHtmlCache>();
        fetcher ??= Substitute.For<IPageFetcher>();
        interactiveFetcher ??= Substitute.For<IInteractivePageFetcher>();
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
        return new BetclicScraper(cache, fetcher, interactiveFetcher, baseOpts, betclicOpts, logger);
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
        result[0].Date.Should().Be("Sob. 17/01");
        result[0].Time.Should().Be("13:30");
        result[0].HomeOdds.Should().Be(2.10);
        result[0].DrawOdds.Should().Be(3.40);
        result[0].AwayOdds.Should().Be(3.20);
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
    public async Task GetUpcomingGamesAsync_WhenCacheReturnsMinimalFixture_ParsesGames()
    {
        // Arrange
        var html = MinimalUpcomingGamesHtml();
        var cache = Substitute.For<IHtmlCache>();
        cache.LoadAsync(PremierLeagueUrl, Arg.Any<CancellationToken>()).Returns(html);
        var fetcher = Substitute.For<IPageFetcher>();
        var interactiveFetcher = Substitute.For<IInteractivePageFetcher>();
        var sut = CreateScraper(cache, fetcher, interactiveFetcher);

        // Act
        var result = await sut.GetUpcomingGamesAsync();

        // Assert
        result.Should().NotBeEmpty();
        result[0].HomeTeam.Should().Be("Arsenal");
        result[0].AwayTeam.Should().Be("Chelsea");
        result[0].Url.Should().NotBeNullOrEmpty();
        await fetcher.DidNotReceive().GetHtmlAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
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
    public async Task GetMatchEventsAsync_WhenCacheReturnsMatchFixture_ParsesEvents()
    {
        // Arrange
        var html = FixtureHelper.LoadFixtureText("betclic/match_page.html");
        html.Should().NotBeNull("fixture file must exist");
        var gameUrl = "https://www.betclic.pl/pilka-nozna-sfootball/premier-league-c3/bournemouth-liverpool-m905675307745280";
        var cache = Substitute.For<IHtmlCache>();
        cache.LoadAsync(gameUrl, Arg.Any<CancellationToken>()).Returns(html!);
        var fetcher = Substitute.For<IPageFetcher>();
        var interactiveFetcher = Substitute.For<IInteractivePageFetcher>();
        var sut = CreateScraper(cache, fetcher, interactiveFetcher);

        // Act
        var result = await sut.GetMatchEventsAsync(gameUrl, expand: false);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(e => e is BookmakerEvent);
        result.Should().Contain(e => !string.IsNullOrEmpty(e.Title) && e.Options.Count > 0);
        await fetcher.DidNotReceive().GetHtmlAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMatchEventsAsync_WhenExpandTrueAndCacheHasContent_ReturnsCachedAndDoesNotCallInteractiveFetcher()
    {
        // Arrange
        var html = FixtureHelper.LoadFixtureText("betclic/match_page.html");
        html.Should().NotBeNull("fixture file must exist");
        var gameUrl = "https://www.betclic.pl/some-match";
        var cache = Substitute.For<IHtmlCache>();
        cache.LoadAsync(gameUrl, Arg.Any<CancellationToken>()).Returns(html!);
        var fetcher = Substitute.For<IPageFetcher>();
        var interactiveFetcher = Substitute.For<IInteractivePageFetcher>();
        var sut = CreateScraper(cache, fetcher, interactiveFetcher);

        // Act
        var result = await sut.GetMatchEventsAsync(gameUrl, expand: true);

        // Assert
        result.Should().NotBeEmpty();
        await interactiveFetcher.DidNotReceive().GetHtmlAfterInteractionsAsync(
            Arg.Any<string>(), Arg.Any<IReadOnlyList<InteractionStep>>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }
}
