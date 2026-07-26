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
          <a href="https://www.flashscore.com/match/football/rakow-AAA/wisla-BBB/?mid=hEbn5dmd" class="eventRowLink"></a>
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
          <a href="/match/football/widzew-CCC/motor-DDD/?mid=KKjYSau3" class="eventRowLink"></a>
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
    rakow.DetailUrl.Should().Be("https://www.flashscore.com/match/football/rakow-AAA/wisla-BBB/?mid=hEbn5dmd");

    var widzew = result.Should().ContainSingle(r => r.ExternalId == "KKjYSau3").Subject;
    widzew.HomeTeam.Should().Be("Widzew Lodz");
    widzew.AwayTeam.Should().Be("Motor Lublin");
    widzew.HomeGoals.Should().Be(2);
    widzew.AwayGoals.Should().Be(2);
    widzew.DetailUrl.Should().Be("https://www.flashscore.com/match/football/widzew-CCC/motor-DDD/?mid=KKjYSau3");
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

  [Fact]
  public void ToNegativeSoccerdataId_IsStableNegativeAndDistinctFromZero()
  {
    // Act
    var id1 = FlashscoreScraper.ToNegativeSoccerdataId("hpalDHSb");
    var id2 = FlashscoreScraper.ToNegativeSoccerdataId("hpalDHSb");
    var other = FlashscoreScraper.ToNegativeSoccerdataId("Cv4o1HuL");

    // Assert
    id1.Should().Be(id2);
    id1.Should().BeNegative();
    id1.Should().NotBe(0);
    other.Should().BeNegative().And.NotBe(id1);
  }

  [Fact]
  public async Task ParseMatchEventsAsync_ParsesGoalAssistSubCardsPenaltyOwnGoalAndIgnoresVar()
  {
    // Arrange
    var sut = CreateScraper();
    var html = """
      <div class="smv__verticalSections">
        <div class="smv__participantRow smv__awayParticipant">
          <div class="smv__incident">
            <div class="smv__timeBox">14'</div>
            <div class="smv__incidentIcon"><svg class="card-ico yellowCard-ico"><title>Foul</title></svg></div>
            <a href="/player/kobacki-olaf/hpalDHSb/" class="smv__playerName"><div>Kobacki O.</div></a>
          </div>
        </div>
        <div class="smv__participantRow smv__homeParticipant">
          <div class="smv__incident">
            <div class="smv__timeBox">24'</div>
            <div class="smv__incidentIconSub">
              <svg data-testid="wcl-icon-incidents-substitution"></svg>
            </div>
            <a href="/player/napieraj-jerzy/Cv4o1HuL/" class="smv__playerName">Napieraj J.</a>
            <div class="smv__incidentSubOut">
              <a href="/player/svarnas-stratos/Mgd8FcOp/" class="smv__subDown smv__playerName">Svarnas S.</a>
            </div>
          </div>
        </div>
        <div class="smv__participantRow smv__awayParticipant">
          <div class="smv__incident">
            <div class="smv__timeBox">37'</div>
            <div class="smv__incidentIcon">
              <svg data-testid="wcl-icon-incidents-goal-soccer"></svg>
            </div>
            <a href="/player/flis-marcin/YRMnFk9t/" class="smv__playerName"><div>Flis M.</div></a>
            <div class="smv__assist smv__assistAway">(<a href="/player/grzesik-jan/6JF3nxxi/">Grzesik J.</a>)</div>
          </div>
        </div>
        <div class="smv__participantRow smv__homeParticipant">
          <div class="smv__incident">
            <div class="smv__timeBox">68'</div>
            <div class="smv__incidentIcon">
              <svg data-testid="wcl-icon-incidents-penalty-goal"></svg>
            </div>
            <a href="/player/makuch-patryk/bmXn8ftP/" class="smv__playerName"><div>Makuch P.</div></a>
          </div>
        </div>
        <div class="smv__participantRow smv__homeParticipant">
          <div class="smv__incident">
            <div class="smv__timeBox">20'</div>
            <div class="smv__incidentIcon">
              <svg data-testid="wcl-icon-incidents-goal-soccer" class="footballOwnGoal-ico"></svg>
            </div>
            <a href="/player/pena-biafore-leonardo/8fXRJkKu/" class="smv__playerName"><div>Pena Biafore L.</div></a>
          </div>
        </div>
        <div class="smv__participantRow smv__awayParticipant">
          <div class="smv__incident">
            <div class="smv__timeBox">50'</div>
            <div class="smv__incidentIcon"><svg class="card-ico redCard-ico"><title>Red Card</title></svg></div>
            <a href="/player/canto-gustavo/S6mWNGd5/" class="smv__playerName"><div>Canto G.</div></a>
          </div>
        </div>
        <div class="smv__participantRow smv__awayParticipant">
          <div class="smv__incident">
            <div class="smv__timeBox">90+1'</div>
            <div class="smv__incidentIcon">
              <svg data-testid="wcl-icon-incidents-red-card-second"></svg>
            </div>
            <a href="/player/maigaard-mikkel/zmeIvZTf/" class="smv__playerName"><div>Maigaard M.</div></a>
          </div>
        </div>
        <div class="smv__participantRow smv__awayParticipant">
          <div class="smv__incident">
            <div class="smv__timeBox">90+2'</div>
            <div class="smv__incidentIcon"><svg class="card-ico yellowCard-ico"></svg></div>
            <a href="/player/romero-cristian/jBZTWXMn/" class="smv__playerName"><div>Romero C.</div></a>
            <div class="smv__assist smv__assistAway">(Not on pitch, misses next match)</div>
          </div>
        </div>
        <div class="smv__participantRow smv__awayParticipant">
          <div class="smv__incident">
            <div class="smv__timeBox">29'</div>
            <div class="smv__incidentIcon">
              <svg data-testid="wcl-icon-incidents-var"></svg>
            </div>
            <div>Goal Disallowed - offside</div>
            <div class="smv__assist smv__assistAway">(<a href="/player/feiertag-stefan/drH6HQp9/">Feiertag S.</a>)</div>
          </div>
        </div>
      </div>
      """;

    // Act
    var events = await sut.ParseMatchEventsAsync(html);

    // Assert
    events.Should().HaveCount(8);

    var yellow = events.Should().ContainSingle(e => e.EventType == "yellow_card" && e.Player != null && e.Player.Name == "Kobacki O.").Subject;
    yellow.Team.Should().Be("away");
    yellow.EventMinute.Should().Be("14");
    yellow.Player!.Id.Should().Be(FlashscoreScraper.ToNegativeSoccerdataId("hpalDHSb"));

    var sub = events.Should().ContainSingle(e => e.EventType == "substitution").Subject;
    sub.Team.Should().Be("home");
    sub.PlayerIn!.Name.Should().Be("Napieraj J.");
    sub.PlayerOut!.Name.Should().Be("Svarnas S.");
    sub.PlayerIn.Id.Should().Be(FlashscoreScraper.ToNegativeSoccerdataId("Cv4o1HuL"));
    sub.PlayerOut.Id.Should().Be(FlashscoreScraper.ToNegativeSoccerdataId("Mgd8FcOp"));

    var goal = events.Should().ContainSingle(e => e.EventType == "goal").Subject;
    goal.Player!.Name.Should().Be("Flis M.");
    goal.AssistPlayer!.Name.Should().Be("Grzesik J.");
    goal.AssistPlayer.Id.Should().Be(FlashscoreScraper.ToNegativeSoccerdataId("6JF3nxxi"));

    events.Should().ContainSingle(e => e.EventType == "penalty_goal" && e.Player != null && e.Player.Name == "Makuch P.");
    events.Should().ContainSingle(e => e.EventType == "own_goal" && e.Player != null && e.Player.Name == "Pena Biafore L.");
    events.Should().ContainSingle(e => e.EventType == "red_card" && e.Player != null && e.Player.Name == "Canto G.");

    var secondYellow = events.Should().ContainSingle(e => e.EventType == "yellow_red_card").Subject;
    secondYellow.EventMinute.Should().Be("90+1");
    secondYellow.Player!.Name.Should().Be("Maigaard M.");

    var noteYellow = events.Should().ContainSingle(e => e.Player != null && e.Player.Name == "Romero C.").Subject;
    noteYellow.EventType.Should().Be("yellow_card");
    noteYellow.AssistPlayer.Should().BeNull();

    events.Should().NotContain(e => e.EventType.Contains("var", StringComparison.OrdinalIgnoreCase));
  }
}
