using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NoMoreBets.Domain.Entities.Rotowire;
using NoMoreBets.Infrastructure.ExternalClients;
using NoMoreBets.Infrastructure.Storage;
using NoMoreBets.Tests.Helpers;

namespace NoMoreBets.Tests.Infrastructure.ExternalClients;

public class RotowireScraperTests
{
    private static RotowireScraper CreateScraper(
        IHtmlCache? cache = null,
        IPageFetcher? fetcher = null,
        BaseScraperOptions? options = null)
    {
        cache ??= Substitute.For<IHtmlCache>();
        fetcher ??= Substitute.For<IPageFetcher>();
        var opts = Options.Create(options ?? new BaseScraperOptions
        {
            DelaySeconds = 0,
            RetryCount = 3,
            RetryDelaySeconds = 0.01,
            TimeoutSeconds = 15
        });
        var logger = NullLogger<RotowireScraper>.Instance;
        return new RotowireScraper(cache, fetcher, opts, logger);
    }

    [Fact]
    public async Task ParseLineupsAsync_WithRealFixture_ParsesMultipleGames()
    {
        var html = FixtureHelper.LoadFixtureText("rotowire/lineups_page.html");
        if (string.IsNullOrEmpty(html))
            return; // Fixture not present (e.g. clone without large file)
        var sut = CreateScraper();

        var result = await sut.ParseLineupsAsync(html);

        result.Should().NotBeEmpty();
        result.Should().OnlyContain(g => g is GameLineup);
        var first = result[0];
        first.HomeTeam.Should().NotBeNull();
        first.AwayTeam.Should().NotBeNull();
        first.HomeTeam.TeamCode.Should().NotBeNullOrEmpty();
        first.AwayTeam.TeamCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSoccerLineupsAsync_WhenCacheReturnsRealFixture_ParsesAllGames()
    {
        var html = FixtureHelper.LoadFixtureText("rotowire/lineups_page.html");
        if (string.IsNullOrEmpty(html))
            return; // Fixture not present
        var url = "https://www.rotowire.com/soccer/lineups.php";
        var cache = Substitute.For<IHtmlCache>();
        cache.LoadAsync(url, Arg.Any<CancellationToken>()).Returns(html);
        var fetcher = Substitute.For<IPageFetcher>();
        var sut = CreateScraper(cache, fetcher);

        var result = await sut.GetSoccerLineupsAsync();

        result.Should().NotBeEmpty();
        result[0].HomeTeam.Should().NotBeNull();
        result[0].AwayTeam.Should().NotBeNull();
        result[0].HomeTeam.TeamCode.Should().NotBeNullOrEmpty();
        result[0].AwayTeam.TeamCode.Should().NotBeNullOrEmpty();
        await fetcher.DidNotReceive().GetHtmlAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }
}
