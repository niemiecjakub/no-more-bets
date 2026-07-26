using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Infrastructure.Scraping.External.Flashscore;
using NoMoreBets.Infrastructure.Tests.Helpers;

namespace NoMoreBets.Infrastructure.Tests.Scraping.External.Flashscore;

public class FlashscoreScraperTests
{
  private static FlashscoreScraper CreateScraper()
  {
    var pageFetcher = PlaywrightPageFetcherMockHelper.CreateMock();
    var baseOpts = Options.Create(new BaseScraperOptions
    {
      DelaySeconds = 0,
      RetryCount = 3,
      RetryDelaySeconds = 0.01,
      TimeoutSeconds = 15
    });
    return new FlashscoreScraper(pageFetcher, baseOpts, NullLogger<FlashscoreScraper>.Instance);
  }

  private static string MinimalResultsHtml()
  {
    return """
      <div class="leagues--static">
        <div id="g_1_hEbn5dmd" class="event__match event__match--withRowLink event__match--twoLine" data-event-row="true">
          <span data-testid="wcl-stageTime"><span data-testid="wcl-scores-simple-text-01">26.07. 14:45</span></span>
          <div class="event__homeParticipant">
            <span data-testid="wcl-scores-simple-text-01">Rakow</span>
          </div>
          <div class="event__awayParticipant">
            <span data-testid="wcl-scores-simple-text-01">Wisla Plock</span>
          </div>
          <span class="event__score event__score--home">1</span>
          <span class="event__score event__score--away">2</span>
        </div>
        <div id="g_1_KKjYSau3" class="event__match event__match--withRowLink event__match--twoLine" data-event-row="true">
          <span data-testid="wcl-stageTime"><span data-testid="wcl-scores-simple-text-01">26.07. 17:30</span></span>
          <div class="event__homeParticipant">
            <span data-testid="wcl-scores-simple-text-01">Widzew Lodz</span>
          </div>
          <div class="event__awayParticipant">
            <span data-testid="wcl-scores-simple-text-01">Motor Lublin</span>
          </div>
          <span class="event__score event__score--home">2</span>
          <span class="event__score event__score--away">2</span>
        </div>
      </div>
      """;
  }

  [Fact]
  public async Task ParseFinishedResultsAsync_WithMinimalFixture_ParsesScoresAndTeams()
  {
    // Arrange
    var sut = CreateScraper();
    var html = MinimalResultsHtml();

    // Act
    var result = await sut.ParseFinishedResultsAsync(html);

    // Assert
    result.Should().HaveCount(2);

    var rakow = result.Should().ContainSingle(r => r.ExternalId == "hEbn5dmd").Subject;
    rakow.HomeTeam.Should().Be("Rakow");
    rakow.AwayTeam.Should().Be("Wisla Plock");
    rakow.HomeGoals.Should().Be(1);
    rakow.AwayGoals.Should().Be(2);
    rakow.MatchDate.Month.Should().Be(7);
    rakow.MatchDate.Day.Should().Be(26);
    rakow.KickoffTime.Should().Be(new TimeOnly(14, 45));

    var widzew = result.Should().ContainSingle(r => r.ExternalId == "KKjYSau3").Subject;
    widzew.HomeTeam.Should().Be("Widzew Lodz");
    widzew.AwayTeam.Should().Be("Motor Lublin");
    widzew.HomeGoals.Should().Be(2);
    widzew.AwayGoals.Should().Be(2);
  }

  [Fact]
  public async Task ParseFinishedResultsAsync_WhenScoreMissing_SkipsRow()
  {
    // Arrange
    var sut = CreateScraper();
    var html = """
      <div id="g_1_abc123" class="event__match" data-event-row="true">
        <span data-testid="wcl-stageTime">26.07. 14:45</span>
        <div class="event__homeParticipant"><span data-testid="wcl-scores-simple-text-01">Rakow</span></div>
        <div class="event__awayParticipant"><span data-testid="wcl-scores-simple-text-01">Wisla Plock</span></div>
        <span class="event__score event__score--home">-</span>
        <span class="event__score event__score--away">-</span>
      </div>
      """;

    // Act
    var result = await sut.ParseFinishedResultsAsync(html);

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public async Task GetFinishedResultsAsync_WhenLeagueSlugUnknown_ReturnsEmpty()
  {
    // Arrange
    var sut = CreateScraper();

    // Act
    var result = await sut.GetFinishedResultsAsync("unknown-league");

    // Assert
    result.Should().BeEmpty();
  }

  [Theory]
  [InlineData("26.07. 14:45", 7, 26, 14, 45)]
  [InlineData("25.07. 20:15", 7, 25, 20, 15)]
  [InlineData("24.07.", 7, 24, null, null)]
  public void TryParseKickoff_ParsesDateAndOptionalTime(
    string text,
    int month,
    int day,
    int? hour,
    int? minute)
  {
    // Act
    var ok = FlashscoreScraper.TryParseKickoff(text, out var matchDate, out var kickoffTime);

    // Assert
    ok.Should().BeTrue();
    matchDate.Month.Should().Be(month);
    matchDate.Day.Should().Be(day);
    if (hour is null)
      kickoffTime.Should().BeNull();
    else
      kickoffTime.Should().Be(new TimeOnly(hour.Value, minute!.Value));
  }
}
