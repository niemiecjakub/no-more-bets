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
        fetcher.GetHtmlAsync(url, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>(), Arg.Any<bool>(), Arg.Any<string?>()).Returns(Task.FromResult(html!));
        var sut = CreateScraper(fetcher);
        var result = await sut.GetSoccerLineupsAsync("premier-league", CancellationToken.None);
        result.Should().NotBeEmpty();
        result[0].HomeTeam.Should().NotBeNull();
        result[0].AwayTeam.Should().NotBeNull();
        result[0].HomeTeamCode.Should().NotBeNullOrEmpty();
        result[0].AwayTeamCode.Should().NotBeNullOrEmpty();
        await fetcher.Received(1).GetHtmlAsync(url, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>(), Arg.Any<bool>(), Arg.Any<string?>());
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
    public async Task ParseLineupsAsync_AddsSixHoursToRotowireKickoff()
    {
        var year = DateTime.Today.Year;
        var html = BuildMinimalLineupHtml("June 11", "3:00 PM ET", "MEX", "RSA", "Mexico", "South Africa");
        var sut = CreateScraper();

        var result = await sut.ParseLineupsAsync(html);

        result.Should().ContainSingle();
        result[0].Date.Should().Be(new DateTime(year, 6, 11, 21, 0, 0, DateTimeKind.Utc));
        result[0].Time.Should().Be("3:00 PM ET");
    }

    [Fact]
    public async Task ParseLineupsAsync_AddsSixHoursAndRollsKickoffToNextDay()
    {
        var year = DateTime.Today.Year;
        var html = BuildMinimalLineupHtml("June 11", "10:00 PM ET", "KOR", "CZE", "South Korea", "Czech Republic");
        var sut = CreateScraper();

        var result = await sut.ParseLineupsAsync(html);

        result.Should().ContainSingle();
        result[0].Date.Should().Be(new DateTime(year, 6, 12, 4, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task ParseLineupsAsync_WithRealFixture_ParsesFirstKickoffWithSixHourOffset()
    {
        var html = FixtureHelper.LoadFixtureText("rotowire/lineups_page.html");
        html.Should().NotBeNull("fixture file must exist");
        var sut = CreateScraper();
        var year = DateTime.Today.Year;

        var result = await sut.ParseLineupsAsync(html!);

        result.Should().NotBeEmpty();
        result[0].Date.Should().Be(new DateTime(year, 6, 11, 21, 0, 0, DateTimeKind.Utc));
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

    private static string BuildMinimalLineupHtml(
        string dateLabel,
        string timeLabel,
        string homeCode,
        string awayCode,
        string homeName,
        string awayName)
    {
        return $"""
            <html><body>
            <div class="lineup is-soccer">
              <div class="lineup__time"><b>{dateLabel}</b>&nbsp; {timeLabel}</div>
              <div class="lineup__team is-home"><div class="lineup__abbr">{homeCode}</div></div>
              <div class="lineup__team is-visit"><div class="lineup__abbr">{awayCode}</div></div>
              <div class="lineup__mteam is-home">{homeName}</div>
              <div class="lineup__mteam is-visit">{awayName}</div>
              <ul class="lineup__list is-home">
                <li class="lineup__status is-expected">Predicted Lineup</li>
                {string.Join('\n', Enumerable.Range(1, 11).Select(i => "<li class=\"lineup__player\"><div class=\"lineup__pos\">GK</div><a>Home Player</a></li>"))}
              </ul>
              <ul class="lineup__list is-visit">
                <li class="lineup__status is-expected">Predicted Lineup</li>
                {string.Join('\n', Enumerable.Range(1, 11).Select(i => "<li class=\"lineup__player\"><div class=\"lineup__pos\">GK</div><a>Away Player</a></li>"))}
              </ul>
            </div>
            </body></html>
            """;
    }
}
