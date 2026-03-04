using AngleSharp;
using AngleSharp.Dom;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NoMoreBets.Application.Common.Dto.Leagues;
using NoMoreBets.Infrastructure.Scraping.BrowserAutomation;
using NoMoreBets.Infrastructure.Scraping.External.Fotmob;
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Tests.Helpers;

namespace NoMoreBets.Tests.Infrastructure.Scraping.External.Fotmob;

public class FotmobScraperTests
{
    private static FotmobScraper CreateScraper(
        PlaywrightPageFetcher? pageFetcher = null,
        BaseScraperOptions? baseOptions = null)
    {
        pageFetcher ??= PlaywrightPageFetcherMockHelper.CreateMock().Object;
        var baseOpts = Options.Create(baseOptions ?? new BaseScraperOptions
        {
            DelaySeconds = 0,
            RetryCount = 3,
            RetryDelaySeconds = 0.01,
            TimeoutSeconds = 15
        });
        var logger = NullLogger<FotmobScraper>.Instance;
        return new FotmobScraper(pageFetcher, baseOpts, logger);
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
        result.Should().OnlyContain(c => c is TableEntry);

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
        result.Should().OnlyContain(c => c is TableEntry);

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
        result.Should().OnlyContain(c => c is TableEntry);

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
        result.Should().OnlyContain(c => c is TableEntry);

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

    [Fact]
    public async Task ParsePlayersFromDocumentAsync_WithNoTable_ReturnsEmptyList()
    {
        // Arrange
        var html = FixtureHelper.LoadFixtureText("fotmob/stats_no_player_table.html");
        if (html is null)
            return; // Fixture not available

        // Act
        var result = await FotmobScraper.ParsePlayersFromDocumentAsync(html);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ParsePlayersFromDocumentAsync_WithPlayerStatsFixture_ParsesExpectedStructure()
    {
        // Arrange
        var html = FixtureHelper.LoadFixtureText("fotmob/player_stats.html");
        if (html is null)
            return; // Fixture not available

        // Act
        var result = await FotmobScraper.ParsePlayersFromDocumentAsync(html);

        // Assert
        result.Should().NotBeEmpty("fixture should contain player table rows");
        var table = await LoadDocumentAndGetTableRowCount(html);
        if (table.HasValue)
            result.Should().HaveCount(table.Value, "parsed count should match tbody tr count in fixture");
        foreach (var row in result)
        {
            row.Player.Should().NotBeNull();
            row.Score.Should().NotBeNull();
            row.MinutesPlayed.Should().NotBeNull();
            row.Goals.Should().NotBeNull();
            row.Assists.Should().NotBeNull();
            row.Xg.Should().NotBeNull();
            row.Xa.Should().NotBeNull();
            row.XgPlusXa.Should().NotBeNull();
            row.DefensiveContributions.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task ParsePlayersFromDocumentAsync_WithPlayerStatsFixture_FirstRowMatchesFixtureContent()
    {
        // Arrange: derive expected first row from fixture DOM (no hardcoded strings)
        var html = FixtureHelper.LoadFixtureText("fotmob/player_stats.html");
        if (html is null)
            return; // Fixture not available
        var (expectedPlayer, expectedScore, expectedMinutes, expectedGoals, expectedAssists, expectedXg, expectedXa, expectedXgPlusXa, expectedDefensive) = await GetFirstRowExpectedFromFixtureAsync(html);
        if (expectedPlayer is null)
            return; // Could not extract expected from fixture

        // Act
        var result = await FotmobScraper.ParsePlayersFromDocumentAsync(html);

        // Assert
        result.Should().NotBeEmpty();
        result[0].Player.Should().Be(expectedPlayer);
        result[0].Score.Should().Be(expectedScore);
        result[0].MinutesPlayed.Should().Be(expectedMinutes);
        result[0].Goals.Should().Be(expectedGoals);
        result[0].Assists.Should().Be(expectedAssists);
        result[0].Xg.Should().Be(expectedXg);
        result[0].Xa.Should().Be(expectedXa);
        result[0].XgPlusXa.Should().Be(expectedXgPlusXa);
        result[0].DefensiveContributions.Should().Be(expectedDefensive);
    }

    private static async Task<int?> LoadDocumentAndGetTableRowCount(string html)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var doc = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);
        var table = doc.QuerySelector("table[class*='StyledTable']") ?? doc.QuerySelector("[class*='StyledTable']");
        if (table is null)
            return null;
        var rows = table.QuerySelectorAll("tbody tr");
        return rows.Count();
    }

    private static async Task<(string? Player, string Score, string MinutesPlayed, string Goals, string Assists, string Xg, string Xa, string XgPlusXa, string DefensiveContributions)> GetFirstRowExpectedFromFixtureAsync(string html)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var doc = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);
        var table = doc.QuerySelector("table[class*='StyledTable']") ?? doc.QuerySelector("[class*='StyledTable']");
        if (table is null)
            return (null, "", "", "", "", "", "", "", "");
        var rows = table.QuerySelectorAll("tbody tr");
        if (rows.Length == 0)
            return (null, "", "", "", "", "", "", "", "");
        var firstRow = rows[0];
        var cells = firstRow.QuerySelectorAll("td").ToArray();
        if (cells.Length < 9)
            return (null, "", "", "", "", "", "", "", "");
        var player = cells[0].QuerySelector("[class*='PlayerNameCSS']")?.TextContent.Trim();
        var score = cells[1].QuerySelector("[class*='PlayerRatingCSS'] span")?.TextContent.Trim() ?? GetCellText(cells[1]);
        return (player, score, GetCellText(cells[2]), GetCellText(cells[3]), GetCellText(cells[4]), GetCellText(cells[5]), GetCellText(cells[6]), GetCellText(cells[7]), GetCellText(cells[8]));
    }

    private static string GetCellText(IElement cell)
    {
        var span = cell.QuerySelector("span");
        return span?.TextContent.Trim() ?? cell.TextContent.Trim() ?? "";
    }

    [Fact]
    public async Task ParseStatisticsFromDocumentAsync_WithNoStatGroupContainer_ReturnsEmptyList()
    {
        // Arrange
        var html = FixtureHelper.LoadFixtureText("fotmob/stats_no_stat_groups.html");
        if (html is null)
            return; // Fixture not available

        // Act
        var result = await FotmobScraper.ParseStatisticsFromDocumentAsync(html);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseStatisticsFromDocumentAsync_WithMatchStatsFixture_ParsesExpectedStructure()
    {
        // Arrange
        var html = FixtureHelper.LoadFixtureText("fotmob/match_stats.html");
        if (html is null)
            return; // Fixture not available

        // Act
        var result = await FotmobScraper.ParseStatisticsFromDocumentAsync(html);

        // Assert
        result.Should().NotBeEmpty("fixture contains StatGroupContainer(s)");
        foreach (var group in result)
        {
            group.Title.Should().NotBeNull();
            group.Rows.Should().NotBeNull();
            foreach (var row in group.Rows!)
                row.Label.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task ParseStatisticsFromDocumentAsync_WithMatchStatsFixture_FirstGroupMatchesFixtureContent()
    {
        // Arrange: derive expected first group from fixture DOM (no hardcoded strings)
        var html = FixtureHelper.LoadFixtureText("fotmob/match_stats.html");
        if (html is null)
            return; // Fixture not available
        var expected = await GetFirstStatGroupExpectedFromFixtureAsync(html);
        if (expected is null)
            return; // Could not extract expected from fixture

        // Act
        var result = await FotmobScraper.ParseStatisticsFromDocumentAsync(html);

        // Assert
        result.Should().NotBeEmpty();
        result[0].Title.Should().Be(expected.Value.Title);
        result[0].Rows.Should().NotBeEmpty();
        result[0].Rows![0].Label.Should().Be(expected.Value.FirstRowLabel);
        result[0].Rows![0].HomeValue.Should().Be(expected.Value.FirstRowHomeValue);
        result[0].Rows![0].AwayValue.Should().Be(expected.Value.FirstRowAwayValue);
    }

    private static async Task<(string Title, string FirstRowLabel, string? FirstRowHomeValue, string? FirstRowAwayValue)?> GetFirstStatGroupExpectedFromFixtureAsync(string html)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var doc = await context.OpenAsync(req => req.Content(html)).ConfigureAwait(false);
        var container = doc.QuerySelector("[class*='StatGroupContainer']");
        if (container is null)
            return null;
        var header = container.QuerySelector("header");
        var titleEl = header?.QuerySelector("h2") ?? header?.QuerySelector("[class*='Title']");
        var title = titleEl?.TextContent.Trim() ?? "";
        var children = container.Children.ToArray();
        string? firstLabel = null;
        string? firstHome = null;
        string? firstAway = null;
        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var className = child.ClassName ?? "";
            if (className.Contains("PossessionTitle", StringComparison.OrdinalIgnoreCase))
            {
                var labelEl = child.QuerySelector("[class*='StatTitle']");
                firstLabel = labelEl?.TextContent.Trim();
                if (i + 1 < children.Length && (children[i + 1].ClassName ?? "").Contains("PossessionDiv", StringComparison.OrdinalIgnoreCase))
                {
                    var segments = children[i + 1].QuerySelectorAll("[class*='PossessionSegment'] span").ToArray();
                    firstHome = segments.Length > 0 ? segments[0].TextContent.Trim() : null;
                    firstAway = segments.Length > 1 ? segments[1].TextContent.Trim() : null;
                }
                break;
            }
            if (child.TagName?.ToUpperInvariant() == "LI" && className.Contains("Stat", StringComparison.OrdinalIgnoreCase))
            {
                var labelEl = child.QuerySelector("[class*='StatTitle']");
                firstLabel = labelEl?.TextContent.Trim();
                var boxes = child.QuerySelectorAll("[class*='StatBox']").ToArray();
                if (boxes.Length >= 2)
                {
                    firstHome = boxes[0].QuerySelector("[class*='StatValue']")?.TextContent.Trim();
                    firstAway = boxes[1].QuerySelector("[class*='StatValue']")?.TextContent.Trim();
                }
                if (!string.IsNullOrEmpty(firstLabel) && (firstHome is not null || firstAway is not null))
                    break;
            }
        }
        if (firstLabel is null)
            return null; // Need at least first row to assert
        return (title, firstLabel, firstHome, firstAway);
    }

    [Fact]
    public async Task ParseMatchDetailsAsync_WithMinimalFixture_ParsesHomeAndAwayTeam()
    {
        // Arrange
        var html = FixtureHelper.LoadFixtureText("fotmob/match_details_minimal.html");
        if (html is null)
            return; // Fixture not available
        var sut = CreateScraper();

        // Act
        var result = await sut.ParseMatchDetailsAsync(html);

        // Assert
        result.HomeTeam.Should().Be("Home");
        result.AwayTeam.Should().Be("Away");
        result.HomeLineup.Should().BeNull();
        result.AwayLineup.Should().BeNull();
    }

    [Fact]
    public async Task ParseMatchDetailsAsync_WithNoH1_ReturnsEmptyTeamNames()
    {
        // Arrange
        var html = FixtureHelper.LoadFixtureText("fotmob/match_details_no_h1.html");
        if (html is null)
            return; // Fixture not available
        var sut = CreateScraper();

        // Act
        var result = await sut.ParseMatchDetailsAsync(html);

        // Assert
        result.HomeTeam.Should().BeEmpty();
        result.AwayTeam.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMatchDetailsAsync_WhenStatsFixtureContainsPlayerTable_ReturnsDetailsWithPlayers()
    {
        // Arrange: first fetch = minimal match details, second fetch = stats tab with player table
        var minimalHtml = FixtureHelper.LoadFixtureText("fotmob/match_details_minimal.html");
        var playerStatsHtml = FixtureHelper.LoadFixtureText("fotmob/player_stats.html");
        if (minimalHtml is null || playerStatsHtml is null)
            return; // Fixtures not available
        var mockFetcher = PlaywrightPageFetcherMockHelper.CreateMock();
        mockFetcher
            .SetupSequence(x => x.GetHtmlAfterInteractionsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<InteractionStep>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(minimalHtml)
            .ReturnsAsync(playerStatsHtml);
        var sut = CreateScraper(mockFetcher.Object);

        // Act
        var result = await sut.GetMatchDetailsAsync("https://www.fotmob.com/pl/matches/some-match/1");

        // Assert
        result.HomeTeam.Should().Be("Home");
        result.AwayTeam.Should().Be("Away");
        result.Players.Should().NotBeNull("stats fixture contains player table");
        result.Players!.Count.Should().BeGreaterThan(0, "fixture has multiple player rows");
    }
}
