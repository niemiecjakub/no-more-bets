using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NoMoreBets.Features.Fotmob.Model;
using NoMoreBets.Features.Fotmob.Scraping;
using NoMoreBets.Infrastructure.Fetching;
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Infrastructure.Storage;
using NoMoreBets.Tests.Helpers;

namespace NoMoreBets.Tests.Features.Fotmob.Scraping;

public class FotmobScraperTests
{
    private static FotmobScraper CreateScraper(
        IHtmlCache? cache = null,
        IPageFetcher? fetcher = null,
        BaseScraperOptions? baseOptions = null,
        FotmobScraperOptions? fotmobOptions = null)
    {
        cache ??= Substitute.For<IHtmlCache>();
        fetcher ??= Substitute.For<IPageFetcher>();
        var baseOpts = Options.Create(baseOptions ?? new BaseScraperOptions
        {
            DelaySeconds = 0,
            RetryCount = 3,
            RetryDelaySeconds = 0.01,
            TimeoutSeconds = 15
        });
        var fotmobOpts = Options.Create(fotmobOptions ?? new FotmobScraperOptions
        {
            LeagueId = 47,
            LeagueSlug = "premier-league"
        });
        var logger = NullLogger<FotmobScraper>.Instance;
        return new FotmobScraper(cache, fetcher, baseOpts, fotmobOpts, logger);
    }

    [Fact]
    public async Task ParseLeagueTableClubsAsync_WithNoTableContainer_ThrowsInvalidOperationException()
    {
        // Arrange
        var html = "<html><body><div>No table</div></body></html>";
        var sut = CreateScraper();

        // Act
        var act = () => sut.ParseLeagueTableClubsAsync(html);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Table container not found*");
    }


    [Fact]
    public async Task ParseXgStatsAsync_WithNoTableContainer_ThrowsInvalidOperationException()
    {
        // Arrange
        var html = "<html><body><div>No table</div></body></html>";
        var sut = CreateScraper();

        // Act
        var act = () => sut.ParseXgStatsAsync(html);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Table container not found*");
    }

    [Fact]
    public async Task ParseLeagueTableClubsAsync_WithFixtureFile_ParsesExpectedStructure()
    {
        // Arrange
        var html = FixtureHelper.LoadFixtureText("fotmob/table.html");
        if (html is null)
            return; // Fixture not available (e.g. not copied to output)
        var sut = CreateScraper();

        // Act
        var result = await sut.ParseLeagueTableClubsAsync(html);

        // Assert
        result.Should().NotBeEmpty("fixture should contain at least one table row");
        result.Should().OnlyContain(c => c is Club);

        foreach (var club in result)
        {
            club.Position.Should().BeInRange(1, result.Count, "positions should be 1-based and sequential");
            club.TeamName.Should().NotBeNullOrWhiteSpace("every club should have a team name");
            club.TeamShortname.Should().NotBeNullOrWhiteSpace("every club should have a short name");
            club.TeamId.Should().BePositive("team ID should be extracted from URL");
            club.MatchesPlayed.Should().BeGreaterThanOrEqualTo(0, "matches played cannot be negative");
            club.Wins.Should().BeGreaterThanOrEqualTo(0);
            club.Draws.Should().BeGreaterThanOrEqualTo(0);
            club.Losses.Should().BeGreaterThanOrEqualTo(0);
            (club.Wins + club.Draws + club.Losses).Should().BeLessThanOrEqualTo(club.MatchesPlayed,
                "wins + draws + losses cannot exceed matches played");
            club.GoalsFor.Should().BeGreaterThanOrEqualTo(0);
            club.GoalsAgainst.Should().BeGreaterThanOrEqualTo(0);
            club.Points.Should().BeGreaterThanOrEqualTo(0);
            if (club.Form.Length > 0)
                club.Form.Should().MatchRegex("^[WDL]+$", "form should only contain W, D, L");
        }

        result.Select(c => c.Position).Should().BeInAscendingOrder("positions should be ordered 1, 2, 3, ...");
        result.Select(c => c.Position).Should().OnlyHaveUniqueItems("each position should appear once");
        result.Select(c => c.TeamId).Should().OnlyHaveUniqueItems("each team ID should appear once");
    }
}
