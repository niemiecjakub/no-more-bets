using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Features.Rotowire.Model;
using NoMoreBets.Features.Rotowire.Scraping;
using NoMoreBets.Infrastructure.Fetching;
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Tests.Helpers;

namespace NoMoreBets.Tests.Features.Rotowire.Scraping;

public class RotowireScraperTests
{
    private static RotowireScraper CreateScraper(
        PlaywrightPageFetcher? pageFetcher = null,
        BaseScraperOptions? options = null)
    {
        pageFetcher ??= new Mock<PlaywrightPageFetcher>(NullLogger<PlaywrightPageFetcher>.Instance).Object;
        var opts = Options.Create(options ?? new BaseScraperOptions
        {
            DelaySeconds = 0,
            RetryCount = 3,
            RetryDelaySeconds = 0.01,
            TimeoutSeconds = 15
        });
        var logger = NullLogger<RotowireScraper>.Instance;
        return new RotowireScraper(pageFetcher, opts, logger);
    }

    [Fact]
    public async Task ParseLineupsAsync_WithRealFixture_ParsesMultipleGames()
    {
        var html = FixtureHelper.LoadFixtureText("rotowire/lineups_page.html");
        html.Should().NotBeNull("fixture file must exist");
        var sut = CreateScraper();

        var result = await sut.ParseLineupsAsync(html!);

        result.Should().NotBeEmpty();
        result.Should().OnlyContain(g => g is GameLineup);
        var first = result[0];
        first.HomeTeam.Should().NotBeNull();
        first.AwayTeam.Should().NotBeNull();
        first.HomeTeamCode.Should().NotBeNullOrEmpty();
        first.AwayTeamCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSoccerLineupsAsync_WhenFetcherReturnsRealFixture_ParsesAllGames()
    {
        var html = FixtureHelper.LoadFixtureText("rotowire/lineups_page.html");
        html.Should().NotBeNull("fixture file must exist");
        var url = "https://www.rotowire.com/soccer/lineups.php";
        var fetcherMock = new Mock<PlaywrightPageFetcher>(NullLogger<PlaywrightPageFetcher>.Instance);
        fetcherMock.Setup(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).ReturnsAsync(html!);
        var sut = CreateScraper(fetcherMock.Object);

        var result = await sut.GetSoccerLineupsAsync();

        result.Should().NotBeEmpty();
        result[0].HomeTeam.Should().NotBeNull();
        result[0].AwayTeam.Should().NotBeNull();
        result[0].HomeTeamCode.Should().NotBeNullOrEmpty();
        result[0].AwayTeamCode.Should().NotBeNullOrEmpty();
        fetcherMock.Verify(f => f.GetHtmlAsync(url, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ParseLineupsAsync_WithRealFixture_ParsesInjuriesWithInjuryStatusEnum()
    {
        // Arrange
        var html = FixtureHelper.LoadFixtureText("rotowire/lineups_page.html");
        html.Should().NotBeNull("fixture file must exist");
        var sut = CreateScraper();

        // Act
        var result = await sut.ParseLineupsAsync(html!);

        // Assert
        result.Should().NotBeEmpty();
        var allInjuries = result
            .SelectMany(g => g.HomeTeam.Injuries.Concat(g.AwayTeam.Injuries))
            .ToList();
        allInjuries.Should().NotBeEmpty("fixture contains injuries with QUES, SUS, OUT");
        allInjuries.Should().OnlyContain(e =>
            e.Status == InjuryStatus.Out || e.Status == InjuryStatus.Questionable ||
            e.Status == InjuryStatus.Suspended || e.Status == InjuryStatus.Unknown,
            "all statuses must be InjuryStatus enum values");
        allInjuries.Should().Contain(e => e.Status == InjuryStatus.Out);
        allInjuries.Should().Contain(e => e.Status == InjuryStatus.Questionable);
    }
}
