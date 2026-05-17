using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Infrastructure.Scraping.BrowserAutomation;
using NoMoreBets.Infrastructure.Scraping.External.Rotowire;
using NoMoreBets.Infrastructure.Tests.Helpers;

namespace NoMoreBets.Infrastructure.Tests.Scraping.External.Rotowire;

public class RotowireScraperTests
{
    private static RotowireScraper CreateScraper(
        PlaywrightPageFetcher? pageFetcher = null,
        BaseScraperOptions? options = null)
    {
        pageFetcher ??= PlaywrightPageFetcherMockHelper.CreateMock();
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
    public async Task ParseLineupsAsync_WithNoLineupSections_ReturnsEmpty()
    {
        var html = "<html><body><div>No lineup sections</div></body></html>";
        var sut = CreateScraper();

        var result = await sut.ParseLineupsAsync(html);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseLineupsAsync_WithRealFixture_ParsesMultipleGames()
    {
        var html = FixtureHelper.LoadFixtureText("rotowire/lineups_page.html");
        html.Should().NotBeNull("fixture file must exist");
        var sut = CreateScraper();

        var result = await sut.ParseLineupsAsync(html!);

        result.Should().NotBeEmpty();
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
        var fetcher = PlaywrightPageFetcherMockHelper.CreateMock();
        fetcher.GetHtmlAsync(url, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(html!));
        var sut = CreateScraper(fetcher);

        var result = await sut.GetSoccerLineupsAsync("premier-league", CancellationToken.None);

        result.Should().NotBeEmpty();
        result[0].HomeTeam.Should().NotBeNull();
        result[0].AwayTeam.Should().NotBeNull();
        result[0].HomeTeamCode.Should().NotBeNullOrEmpty();
        result[0].AwayTeamCode.Should().NotBeNullOrEmpty();
        await fetcher.Received(1).GetHtmlAsync(url, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSoccerLineupsAsync_WhenLeagueSlugNullOrWhitespace_ThrowsArgumentException()
    {
        var sut = CreateScraper();

        var act = () => sut.GetSoccerLineupsAsync("   ", CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("leagueSlug");
    }

    [Fact]
    public async Task GetSoccerLineupsAsync_WhenLeagueSlugNotInRotowireMap_ThrowsNotSupportedException()
    {
        var sut = CreateScraper();

        var act = () => sut.GetSoccerLineupsAsync("unsupported-league", CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*unsupported-league*");
    }

    [Fact]
    public async Task ParseLineupsAsync_WithRealFixture_ParsesInjuriesWithInjuryStatusEnum()
    {
        var html = FixtureHelper.LoadFixtureText("rotowire/lineups_page.html");
        html.Should().NotBeNull("fixture file must exist");
        var sut = CreateScraper();

        var result = await sut.ParseLineupsAsync(html!);

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
