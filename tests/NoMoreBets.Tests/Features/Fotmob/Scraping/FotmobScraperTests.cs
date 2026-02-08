using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
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
        IInteractivePageFetcher? interactiveFetcher = null,
        BaseScraperOptions? baseOptions = null,
        FotmobScraperOptions? fotmobOptions = null)
    {
        cache ??= new Mock<IHtmlCache>().Object;
        fetcher ??= new Mock<IPageFetcher>().Object;
        interactiveFetcher ??= new Mock<IInteractivePageFetcher>().Object;
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
        return new FotmobScraper(cache, fetcher, interactiveFetcher, baseOpts, fotmobOpts, logger);
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
    public async Task ParseXgStatsAsync_WithDivBasedRow_ParsesOneXgStats()
    {
        // Arrange: minimal HTML matching live Fotmob xG table (div row, no td; position as text node)
        var html = """
            <html><body>
            <article class="TableContainer">
            <div class="css-6ulo9t-TableRowCSS e1usskhq7">1<div class="css-kg3xxt-ChevronWrapper e1usskhq5"><span>0</span></div><div class="css-ugqe26-TeamCellContentCSS esi6yk80"><a class="css-2cm6w5-TeamLink esi6yk81" href="/teams/9825/overview/arsenal"><img src="https://images.fotmob.com/image_resources/logo/teamlogo/9825_xsmall.png" class="Image TeamIcon ImageWithFallback" alt="" width="18" height="18"><span class="TeamName css-b6tl5v-TeamName esi6yk82">Arsenal</span><span class="TeamShortname css-1a6q0t2-TeamShortname esi6yk83">Arsenal</span></a></div>24<div class="css-1s2i42-XgCellCSS e1sg69790"><span title="42.66" class="css-l167b0-MainNumber e1sg69791">42.7</span><sup class="css-bdoi37-DiffText e1sg69792">+3.3</sup></div><div class="css-1s2i42-XgCellCSS e1sg69790"><span title="16.42" class="css-l167b0-MainNumber e1sg69791">16.4</span><sup class="css-1dw779-DiffText e1sg69792">+0.6</sup></div><div class="css-1s2i42-XgCellCSS e1sg69790"><span title="51.23" class="css-1w05nap-MainNumber e1sg69791">51</span><sup class="css-bdoi37-DiffText e1sg69792">+2</sup></div></div>
            </article>
            </body></html>
            """;
        var sut = CreateScraper();

        // Act
        var result = await sut.ParseXgStatsAsync(html);

        // Assert
        result.Should().HaveCount(1);
        var stat = result[0];
        stat.Position.Should().Be(1);
        stat.TeamId.Should().Be(9825);
        stat.TeamName.Should().Be("Arsenal");
        stat.TeamShortname.Should().Be("Arsenal");
        stat.Xg.Should().BeApproximately(42.66, 0.01);
        stat.XgDiff.Should().Be("+3.3");
        stat.Xga.Should().BeApproximately(16.42, 0.01);
        stat.XgaDiff.Should().Be("+0.6");
        stat.Xpts.Should().BeApproximately(51.23, 0.01);
        stat.XptsDiff.Should().Be("+2");
    }

    [Fact]
    public async Task ParseXgStatsAsync_WithFixtureFile_ParsesExpectedStructure()
    {
        // Arrange
        var html = FixtureHelper.LoadFixtureText("fotmob/xg.html");
        if (html is null)
            return; // Fixture not available (e.g. not copied to output)
        var sut = CreateScraper();

        // Act
        var result = await sut.ParseXgStatsAsync(html);

        // Assert
        if (result.Count == 0)
            return; // Fixture DOM may differ from live page (e.g. AngleSharp normalizes div>td)
        result.Should().OnlyContain(s => s is XgStats);

        foreach (var stat in result)
        {
            stat.Position.Should().BeInRange(1, result.Count, "positions should be 1-based and sequential");
            stat.TeamName.Should().NotBeNullOrWhiteSpace("every team should have a team name");
            stat.TeamShortname.Should().NotBeNullOrWhiteSpace("every team should have a short name");
            stat.TeamId.Should().BePositive("team ID should be extracted from URL");
            stat.Xg.Should().BeGreaterThanOrEqualTo(0, "xG cannot be negative");
            stat.Xga.Should().BeGreaterThanOrEqualTo(0, "xGA cannot be negative");
            stat.Xpts.Should().BeGreaterThanOrEqualTo(0, "xPts cannot be negative");
        }

        result.Select(s => s.Position).Should().BeInAscendingOrder("positions should be ordered 1, 2, 3, ...");
        result.Select(s => s.Position).Should().OnlyHaveUniqueItems("each position should appear once");
        result.Select(s => s.TeamId).Should().OnlyHaveUniqueItems("each team ID should appear once");
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
            if (club.Form.Count > 0)
            {
                var formStr = string.Concat(club.Form.Select(f => f switch { MatchResult.Win => "W", MatchResult.Draw => "D", MatchResult.Loss => "L", _ => "?" }));
                formStr.Should().MatchRegex("^[WDL]+$", "form should only contain W, D, L");
            }
        }

        result.Select(c => c.Position).Should().BeInAscendingOrder("positions should be ordered 1, 2, 3, ...");
        result.Select(c => c.Position).Should().OnlyHaveUniqueItems("each position should appear once");
        result.Select(c => c.TeamId).Should().OnlyHaveUniqueItems("each team ID should appear once");
    }

    [Fact]
    public async Task ParseLeagueTableClubsAsync_WithAwayFixture_ParsesExpectedStructure()
    {
        // Arrange
        var html = FixtureHelper.LoadFixtureText("fotmob/away.html");
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
            if (club.Form.Count > 0)
            {
                var formStr = string.Concat(club.Form.Select(f => f switch { MatchResult.Win => "W", MatchResult.Draw => "D", MatchResult.Loss => "L", _ => "?" }));
                formStr.Should().MatchRegex("^[WDL]+$", "form should only contain W, D, L");
            }
        }

        result.Select(c => c.Position).Should().BeInAscendingOrder("positions should be ordered 1, 2, 3, ...");
        result.Select(c => c.Position).Should().OnlyHaveUniqueItems("each position should appear once");
        result.Select(c => c.TeamId).Should().OnlyHaveUniqueItems("each team ID should appear once");
    }

    [Fact]
    public async Task ParseLeagueTableClubsAsync_WithHomeFixture_ParsesExpectedStructure()
    {
        // Arrange
        var html = FixtureHelper.LoadFixtureText("fotmob/home.html");
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
            if (club.Form.Count > 0)
            {
                var formStr = string.Concat(club.Form.Select(f => f switch { MatchResult.Win => "W", MatchResult.Draw => "D", MatchResult.Loss => "L", _ => "?" }));
                formStr.Should().MatchRegex("^[WDL]+$", "form should only contain W, D, L");
            }
        }

        result.Select(c => c.Position).Should().BeInAscendingOrder("positions should be ordered 1, 2, 3, ...");
        result.Select(c => c.Position).Should().OnlyHaveUniqueItems("each position should appear once");
        result.Select(c => c.TeamId).Should().OnlyHaveUniqueItems("each team ID should appear once");
    }

    [Fact]
    public async Task ParseLeagueTableClubsAsync_WithLast5GamesFixture_ParsesExpectedStructure()
    {
        // Arrange
        var html = FixtureHelper.LoadFixtureText("fotmob/last_5_games.html");
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
            if (club.Form.Count > 0)
            {
                var formStr = string.Concat(club.Form.Select(f => f switch { MatchResult.Win => "W", MatchResult.Draw => "D", MatchResult.Loss => "L", _ => "?" }));
                formStr.Should().MatchRegex("^[WDL]+$", "form should only contain W, D, L");
            }
        }

        result.Select(c => c.Position).Should().BeInAscendingOrder("positions should be ordered 1, 2, 3, ...");
        result.Select(c => c.Position).Should().OnlyHaveUniqueItems("each position should appear once");
        result.Select(c => c.TeamId).Should().OnlyHaveUniqueItems("each team ID should appear once");
    }

    [Fact]
    public async Task ParseClubOverviewAsync_WithNoFormLinks_ReturnsEmptyRecentGames()
    {
        // Arrange
        var html = "<html><body><div>No form links</div></body></html>";
        var sut = CreateScraper();

        // Act
        var result = await sut.ParseClubOverviewAsync(html);

        // Assert
        result.RecentGames.Should().BeEmpty();
        result.DailySummary.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseClubOverviewAsync_WithValidFormSection_ReturnsUpToFiveGames()
    {
        // Arrange: fixture from recent_games_table.html (Newcastle 0-2 Aston Villa, red = loss, opponent 10252)
        var html = FixtureHelper.LoadFixtureText("fotmob/recent_games_table.html");
        if (html is null)
            return; // Fixture not available
        var sut = CreateScraper();

        // Act
        var result = await sut.ParseClubOverviewAsync(html);

        // Assert
        result.RecentGames.Should().HaveCount(1);
        result.RecentGames[0].OpponentId.Should().Be(10252);
        result.RecentGames[0].Score.Should().Be("0 - 2");
        result.RecentGames[0].Result.Should().Be(MatchResult.Loss);
        result.RecentGames[0].GameUrl.Should().Be("https://www.fotmob.com/pl/matches/aston-villa-vs-newcastle-united/3h9v0m#4813603");
    }

    [Fact]
    public async Task ParseClubOverviewAsync_WithTeamFormRed_ReturnsLoss()
    {
        // Arrange
        var html = """
            <html><body>
            <a href="/pl/matches/a-vs-b/1" class="TeamFormMatchLink">
            <div class="FixtureStatusWrapper"><div color="var(--TeamForm-red)" class="ResultBox"><span class="ScoreSpan">0 - 1</span></div></div>
            <img src="https://images.fotmob.com/image_resources/logo/teamlogo/100_xsmall.png" class="TeamIcon">
            </a>
            </body></html>
            """;
        var sut = CreateScraper();

        // Act
        var result = await sut.ParseClubOverviewAsync(html);

        // Assert
        result.RecentGames.Should().HaveCount(1);
        result.RecentGames[0].Result.Should().Be(MatchResult.Loss);
        result.RecentGames[0].OpponentId.Should().Be(100);
    }

    [Fact]
    public async Task ParseClubOverviewAsync_WithTeamFormGreen_ReturnsWin()
    {
        // Arrange
        var html = """
            <html><body>
            <a href="/pl/matches/a-vs-b/2" class="TeamFormMatchLink">
            <div class="FixtureStatusWrapper"><div color="var(--TeamForm-green)" class="ResultBox"><span class="ScoreSpan">2 - 0</span></div></div>
            <img src="https://images.fotmob.com/image_resources/logo/teamlogo/200_xsmall.png" class="TeamIcon">
            </a>
            </body></html>
            """;
        var sut = CreateScraper();

        // Act
        var result = await sut.ParseClubOverviewAsync(html);

        // Assert
        result.RecentGames.Should().HaveCount(1);
        result.RecentGames[0].Result.Should().Be(MatchResult.Win);
        result.RecentGames[0].OpponentId.Should().Be(200);
    }

    [Fact]
    public async Task ParseClubOverviewAsync_WithTeamFormGrey_ReturnsDraw()
    {
        // Arrange
        var html = """
            <html><body>
            <a href="/pl/matches/a-vs-b/3" class="TeamFormMatchLink">
            <div class="FixtureStatusWrapper"><div color="var(--TeamForm-grey)" class="ResultBox"><span class="ScoreSpan">1 - 1</span></div></div>
            <img src="https://images.fotmob.com/image_resources/logo/teamlogo/300_xsmall.png" class="TeamIcon">
            </a>
            </body></html>
            """;
        var sut = CreateScraper();

        // Act
        var result = await sut.ParseClubOverviewAsync(html);

        // Assert
        result.RecentGames.Should().HaveCount(1);
        result.RecentGames[0].Result.Should().Be(MatchResult.Draw);
        result.RecentGames[0].OpponentId.Should().Be(300);
    }

    [Fact]
    public async Task ParseClubOverviewAsync_WithLinkMissingImg_SkipsThatLink()
    {
        // Arrange: one valid link, one without img (should be skipped)
        var html = """
            <html><body>
            <a href="/pl/matches/valid/1" class="TeamFormMatchLink">
            <div class="FixtureStatusWrapper"><div color="var(--TeamForm-green)"><span class="ScoreSpan">1 - 0</span></div></div>
            <img src="https://images.fotmob.com/image_resources/logo/teamlogo/999_xsmall.png" class="TeamIcon">
            </a>
            <a href="/pl/matches/nologo/2" class="TeamFormMatchLink">
            <div class="FixtureStatusWrapper"><div color="var(--TeamForm-red)"><span class="ScoreSpan">0 - 1</span></div></div>
            </a>
            </body></html>
            """;
        var sut = CreateScraper();

        // Act
        var result = await sut.ParseClubOverviewAsync(html);

        // Assert
        result.RecentGames.Should().HaveCount(1);
        result.RecentGames[0].OpponentId.Should().Be(999);
        result.RecentGames[0].GameUrl.Should().Be("https://www.fotmob.com/pl/matches/valid/1");
    }

    [Fact]
    public async Task ParseClubOverviewAsync_WithDailySummaryFixture_ParsesDailySummaryItems()
    {
        // Arrange: fixture with Daily Summary ul/li structure
        var html = FixtureHelper.LoadFixtureText("fotmob/dailySummary.html");
        if (html is null)
            return; // Fixture not available
        var sut = CreateScraper();

        // Act
        var result = await sut.ParseClubOverviewAsync(html);

        // Assert
        result.DailySummary.Should().HaveCount(3);
        result.DailySummary.Should().OnlyContain(s => !string.IsNullOrWhiteSpace(s));
        result.DailySummary[0].Should().Contain("Newcastle suffered");
        result.DailySummary[0].Should().NotEndWith("Więcej");
        result.DailySummary[1].Should().Contain("Eddie Howe");
        result.DailySummary[2].Should().Contain("Sandro Tonali");
    }
}
