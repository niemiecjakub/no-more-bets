using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto.Betting;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.AI.Plugins;
using NoMoreBets.Infrastructure.AI.Plugins.Models;

namespace NoMoreBets.Infrastructure.Tests.AI.Plugins;

public class MatchPluginTests
{
  private const int MatchId = 42;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IMatchRepository _matchRepo;
  private readonly IClubRepository _clubRepo;
  private readonly IBettingRepository _bettingRepo;
  private readonly ILogger<MatchPlugin> _logger;
  private readonly MatchPlugin _sut;

  public MatchPluginTests()
  {
    _unitOfWork = Substitute.For<IUnitOfWork>();
    _matchRepo = Substitute.For<IMatchRepository>();
    _clubRepo = Substitute.For<IClubRepository>();
    _bettingRepo = Substitute.For<IBettingRepository>();
    _unitOfWork.Matches.Returns(_matchRepo);
    _unitOfWork.Clubs.Returns(_clubRepo);
    _unitOfWork.Betting.Returns(_bettingRepo);
    _logger = NullLogger<MatchPlugin>.Instance;
    _sut = new MatchPlugin(MatchId, _unitOfWork, _logger);
  }

  private static Lineup BuildLineup(TeamLineup home, TeamLineup away)
  {
    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    return new Lineup
    {
      MatchId = MatchId,
      HomeTeamJson = JsonSerializer.Serialize(home, options),
      AwayTeamJson = JsonSerializer.Serialize(away, options),
      UpdatedAt = DateTime.UtcNow
    };
  }

  private static (Match match, Club homeClub, Club awayClub) BuildMatchWithClubs(
    int matchId,
    int homeClubId,
    int awayClubId,
    string homeName = "Home FC",
    string awayName = "Away FC",
    int? homeGoals = 1,
    int? awayGoals = 0,
    DateTime? matchDate = null)
  {
    var homeClub = new Club { Id = homeClubId, Name = homeName };
    var awayClub = new Club { Id = awayClubId, Name = awayName };
    var match = new Match
    {
      Id = matchId,
      HomeClubId = homeClubId,
      AwayClubId = awayClubId,
      HomeClub = homeClub,
      AwayClub = awayClub,
      HomeGoals = homeGoals,
      AwayGoals = awayGoals,
      MatchDate = matchDate ?? DateTime.UtcNow.AddDays(-1)
    };
    return (match, homeClub, awayClub);
  }

  private static BettingOddsSnapshot BuildSnapshot(
    DateTime snapshotTime,
    int eventTypeId,
    string eventTypeName,
    string eventJson)
  {
    var snapshot = new BettingOddsSnapshot { MatchId = MatchId, SnapshotTime = snapshotTime };
    var row = new BettingOddsSnapshotRow
    {
      EventTypeId = eventTypeId,
      EventJson = eventJson,
      EventTypeEntity = new BettingEventTypeEntity { Id = eventTypeId, Name = eventTypeName }
    };
    snapshot.Rows.Add(row);
    return snapshot;
  }

  // ---- GetLineupsAsync ----

  [Fact]
  public async Task GetLineupsAsync_WhenLineupIsNull_ReturnsNull()
  {
    // Arrange
    _matchRepo.GetLineup(MatchId).Returns((Lineup?)null);

    // Act
    var result = await _sut.GetLineupsAsync();

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task GetLineupsAsync_WhenLineupExists_ReturnsHomeAndAwayWithPlayers()
  {
    // Arrange
    var homeTeam = new TeamLineup
    {
      LineupType = LineupType.Confirmed,
      Players = new List<PlayerInLineup> { new(FootballPosition.GK, "Keeper"), new(FootballPosition.DC, "Defender") },
      Injuries = Array.Empty<InjuryEntry>()
    };
    var awayTeam = new TeamLineup
    {
      LineupType = LineupType.Predicted,
      Players = new List<PlayerInLineup> { new(FootballPosition.MC, "Midfielder") },
      Injuries = Array.Empty<InjuryEntry>()
    };
    var lineup = BuildLineup(homeTeam, awayTeam);
    _matchRepo.GetLineup(MatchId).Returns(lineup);

    // Act
    var result = await _sut.GetLineupsAsync();

    // Assert
    result.Should().NotBeNull();
    result!.Home.LineupType.Should().Be("Confirmed");
    result.Home.Players.Should().HaveCount(2);
    result.Home.Players[0].Name.Should().Be("Keeper");
    result.Home.Players[0].Position.Should().Be("GK");
    result.Away.LineupType.Should().Be("Predicted");
    result.Away.Players.Should().HaveCount(1);
    result.Away.Players[0].Name.Should().Be("Midfielder");
    result.Away.Players[0].Position.Should().Be("MC");
  }

  [Fact]
  public async Task GetLineupsAsync_WhenPlayersAndInjuriesEmpty_ReturnsEmptyLists()
  {
    // Arrange
    var homeTeam = new TeamLineup { LineupType = LineupType.Unknown, Players = [], Injuries = [] };
    var awayTeam = new TeamLineup { LineupType = LineupType.Unknown, Players = [], Injuries = [] };
    var lineup = BuildLineup(homeTeam, awayTeam);
    _matchRepo.GetLineup(MatchId).Returns(lineup);

    // Act
    var result = await _sut.GetLineupsAsync();

    // Assert
    result.Should().NotBeNull();
    result!.Home.Players.Should().BeEmpty();
    result.Away.Players.Should().BeEmpty();
  }

  // ---- GetInjuriesAsync ----

  [Fact]
  public async Task GetInjuriesAsync_WhenLineupIsNull_ReturnsNull()
  {
    // Arrange
    _matchRepo.GetLineup(MatchId).Returns((Lineup?)null);

    // Act
    var result = await _sut.GetInjuriesAsync();

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task GetInjuriesAsync_WhenNoInjuries_ReturnsEmptyInjuryLists()
  {
    // Arrange
    var homeTeam = new TeamLineup { LineupType = LineupType.Confirmed, Players = [], Injuries = [] };
    var awayTeam = new TeamLineup { LineupType = LineupType.Confirmed, Players = [], Injuries = [] };
    var lineup = BuildLineup(homeTeam, awayTeam);
    _matchRepo.GetLineup(MatchId).Returns(lineup);

    // Act
    var result = await _sut.GetInjuriesAsync();

    // Assert
    result.Should().NotBeNull();
    result!.Home.Injuries.Should().BeEmpty();
    result.Away.Injuries.Should().BeEmpty();
  }

  [Fact]
  public async Task GetInjuriesAsync_WhenInjuriesExist_MapsToInjuriedPlayer()
  {
    // Arrange
    var homeTeam = new TeamLineup
    {
      LineupType = LineupType.Confirmed,
      Players = [],
      Injuries = new List<InjuryEntry> { new(FootballPosition.ST, "Striker", InjuryStatus.Out) }
    };
    var awayTeam = new TeamLineup
    {
      LineupType = LineupType.Confirmed,
      Players = [],
      Injuries = new List<InjuryEntry> { new(FootballPosition.MC, "Mid", InjuryStatus.Questionable) }
    };
    var lineup = BuildLineup(homeTeam, awayTeam);
    _matchRepo.GetLineup(MatchId).Returns(lineup);

    // Act
    var result = await _sut.GetInjuriesAsync();

    // Assert
    result.Should().NotBeNull();
    result!.Home.Injuries.Should().HaveCount(1);
    result.Home.Injuries[0].Name.Should().Be("Striker");
    result.Home.Injuries[0].Position.Should().Be("ST");
    result.Home.Injuries[0].InjuryStatus.Should().Be("Out");
    result.Away.Injuries.Should().HaveCount(1);
    result.Away.Injuries[0].Name.Should().Be("Mid");
    result.Away.Injuries[0].InjuryStatus.Should().Be("Questionable");
  }

  // ---- GetMatchPreviewAsync ----

  [Fact]
  public async Task GetMatchPreviewAsync_WhenPreviewIsNull_ReturnsNoPreviewAvailable()
  {
    // Arrange
    _matchRepo.GetMatchPreview(MatchId).Returns((MatchPreview?)null);

    // Act
    var result = await _sut.GetMatchPreviewAsync();

    // Assert
    result.Should().Be("No preview available.");
  }

  [Fact]
  public async Task GetMatchPreviewAsync_WhenPreviewExists_ReturnsMarkdown()
  {
    // Arrange
    var previewItems = new[] { new PreviewContentItem { Name = "h1", Content = "Title" }, new PreviewContentItem { Name = "p", Content = "Paragraph" } };
    var preview = new MatchPreview
    {
      MatchId = MatchId,
      PreviewContentJson = JsonSerializer.Serialize(previewItems, new JsonSerializerOptions(JsonSerializerDefaults.Web))
    };
    _matchRepo.GetMatchPreview(MatchId).Returns(preview);

    // Act
    var result = await _sut.GetMatchPreviewAsync();

    // Assert
    result.Should().NotBeNullOrEmpty();
    result.Should().Contain("Title");
    result.Should().Contain("Paragraph");
  }

  // ---- GetHead2HeadStatsAsync ----

  [Fact]
  public async Task GetHead2HeadStatsAsync_WhenMatchNotFound_ReturnsNull()
  {
    // Arrange
    _matchRepo.GetMatchByIdAsync(MatchId, Arg.Any<CancellationToken>()).Returns((Match?)null);

    // Act
    var result = await _sut.GetHead2HeadStatsAsync();

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task GetHead2HeadStatsAsync_WhenHead2HeadIsNull_ReturnsNull()
  {
    // Arrange
    var (match, _, _) = BuildMatchWithClubs(MatchId, 1, 2);
    _matchRepo.GetMatchByIdAsync(MatchId, Arg.Any<CancellationToken>()).Returns(match);
    _matchRepo.GetHeadToHead(1, 2).Returns((Head2Head?)null);

    // Act
    var result = await _sut.GetHead2HeadStatsAsync();

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task GetHead2HeadStatsAsync_WhenHead2HeadJsonEmpty_ReturnsNull()
  {
    // Arrange
    var (match, _, _) = BuildMatchWithClubs(MatchId, 1, 2);
    _matchRepo.GetMatchByIdAsync(MatchId, Arg.Any<CancellationToken>()).Returns(match);
    _matchRepo.GetHeadToHead(1, 2).Returns(new Head2Head { Head2HeadJson = "" });

    // Act
    var result = await _sut.GetHead2HeadStatsAsync();

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task GetHead2HeadStatsAsync_WhenHead2HeadJsonWhitespace_ReturnsNull()
  {
    // Arrange
    var (match, _, _) = BuildMatchWithClubs(MatchId, 1, 2);
    _matchRepo.GetMatchByIdAsync(MatchId, Arg.Any<CancellationToken>()).Returns(match);
    _matchRepo.GetHeadToHead(1, 2).Returns(new Head2Head { Head2HeadJson = "   " });

    // Act
    var result = await _sut.GetHead2HeadStatsAsync();

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task GetHead2HeadStatsAsync_WhenHead2HeadExists_ReturnsLlmFriendlyH2H()
  {
    // Arrange: valid HeadToHead JSON (camelCase from JsonSerializerDefaults.Web)
    var json = """
    {"team1":{"id":1,"name":"Team One"},"team2":{"id":2,"name":"Team Two"},"stats":{"overall":{"overallGamesPlayed":10,"overallTeam1Wins":4,"overallTeam2Wins":3,"overallDraws":3,"overallTeam1Scored":12,"overallTeam2Scored":10},"team1AtHome":{"team1GamesPlayedAtHome":5,"team1WinsAtHome":2,"team1LossesAtHome":1,"team1DrawsAtHome":2,"team1ScoredAtHome":6,"team1ConcededAtHome":5},"team2AtHome":{"team2GamesPlayedAtHome":5,"team2WinsAtHome":2,"team2LossesAtHome":2,"team2DrawsAtHome":1,"team2ScoredAtHome":5,"team2ConcededAtHome":6}}}
    """;
    var (match, _, _) = BuildMatchWithClubs(MatchId, 1, 2, "Home FC", "Away FC");
    _matchRepo.GetMatchByIdAsync(MatchId, Arg.Any<CancellationToken>()).Returns(match);
    _matchRepo.GetHeadToHead(1, 2).Returns(new Head2Head { Team1Id = 1, Team2Id = 2, Head2HeadJson = json });

    // Act
    var result = await _sut.GetHead2HeadStatsAsync();

    // Assert
    result.Should().NotBeNull();
    result!.Summary.Should().Be("Home FC vs Away FC");
    result.TotalMatches.Should().Be(10);
    result.TotalDraws.Should().Be(3);
    result.TeamA.Name.Should().Be("Home FC");
    result.TeamB.Name.Should().Be("Away FC");
    result.TeamA.TotalWins.Should().Be(4);
    result.TeamA.TotalGoalsScored.Should().Be(12);
    result.TeamA.TotalGoalsConceded.Should().Be(10);
    result.TeamA.HomeWins.Should().Be(2);
    result.TeamA.AwayWins.Should().Be(2); // Team2's home losses
    result.TeamA.WinPercentage.Should().Be(40);
    result.TeamA.AvgGoalsScored.Should().Be(1.2);
    result.TeamA.AvgGoalsConceded.Should().Be(1.0);
    result.TeamB.TotalWins.Should().Be(3);
    result.TeamB.TotalGoalsScored.Should().Be(10);
    result.TeamB.TotalGoalsConceded.Should().Be(12);
    result.TeamB.HomeWins.Should().Be(2);
    result.TeamB.AwayWins.Should().Be(1); // Team1's home losses
    result.TeamB.WinPercentage.Should().Be(30);
    result.TeamB.AvgGoalsScored.Should().Be(1.0);
    result.TeamB.AvgGoalsConceded.Should().Be(1.2);
  }

  [Fact]
  public async Task GetHead2HeadStatsAsync_WhenHomeIsTeam2_MapsTeamAAndTeamBCorrectly()
  {
    // Arrange: match has HomeClubId=2, AwayClubId=1 so home is Team2 in stored H2H (entity Team1Id=1, Team2Id=2)
    var json = """
    {"team1":{"id":1,"name":"Liverpool"},"team2":{"id":2,"name":"Arsenal"},"stats":{"overall":{"overallGamesPlayed":8,"overallTeam1Wins":2,"overallTeam2Wins":4,"overallDraws":2,"overallTeam1Scored":8,"overallTeam2Scored":11},"team1AtHome":{"team1GamesPlayedAtHome":4,"team1WinsAtHome":1,"team1LossesAtHome":2,"team1DrawsAtHome":1,"team1ScoredAtHome":4,"team1ConcededAtHome":6},"team2AtHome":{"team2GamesPlayedAtHome":4,"team2WinsAtHome":2,"team2LossesAtHome":1,"team2DrawsAtHome":1,"team2ScoredAtHome":5,"team2ConcededAtHome":2}}}
    """;
    var (match, _, _) = BuildMatchWithClubs(MatchId, 2, 1, "Arsenal", "Liverpool");
    _matchRepo.GetMatchByIdAsync(MatchId, Arg.Any<CancellationToken>()).Returns(match);
    _matchRepo.GetHeadToHead(2, 1).Returns(new Head2Head { Team1Id = 1, Team2Id = 2, Head2HeadJson = json });

    // Act
    var result = await _sut.GetHead2HeadStatsAsync();

    // Assert: TeamA = home = Arsenal (Team2 in H2H), TeamB = away = Liverpool (Team1 in H2H)
    result.Should().NotBeNull();
    result!.Summary.Should().Be("Arsenal vs Liverpool");
    result.TeamA.Name.Should().Be("Arsenal");
    result.TeamB.Name.Should().Be("Liverpool");
    result.TeamA.TotalWins.Should().Be(4); // overallTeam2Wins
    result.TeamB.TotalWins.Should().Be(2); // overallTeam1Wins
    result.TeamA.HomeWins.Should().Be(2); // Team2WinsAtHome
    result.TeamA.AwayWins.Should().Be(2); // Team1LossesAtHome (when Arsenal played away, Liverpool was at home)
  }

  [Fact]
  public async Task GetHead2HeadStatsAsync_WhenHead2HeadJsonInvalid_ReturnsNull()
  {
    // Arrange: JSON does not match HeadToHead schema, so Stats/Overall will be null
    var (match, _, _) = BuildMatchWithClubs(MatchId, 1, 2);
    _matchRepo.GetMatchByIdAsync(MatchId, Arg.Any<CancellationToken>()).Returns(match);
    _matchRepo.GetHeadToHead(1, 2).Returns(new Head2Head { Team1Id = 1, Team2Id = 2, Head2HeadJson = "{\"meetings\":[]}" });

    // Act
    var result = await _sut.GetHead2HeadStatsAsync();

    // Assert
    result.Should().BeNull();
  }

  // ---- GetClubDailySummaryAsync ----

  [Fact]
  public async Task GetClubDailySummaryAsync_WhenNoSummary_ReturnsNoDailySummaryAvailable()
  {
    // Arrange
    _clubRepo.GetLatestDailySummaryAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((ClubDailySummary?)null);

    // Act
    var result = await _sut.GetClubDailySummaryAsync(1);

    // Assert
    result.Should().Be("No daily summary available.");
  }

  [Fact]
  public async Task GetClubDailySummaryAsync_WhenSummaryExists_ReturnsToString()
  {
    // Arrange
    var summary = new ClubDailySummary { ClubId = 1, Date = new DateOnly(2025, 3, 1), Summary = "Summary text" };
    _clubRepo.GetLatestDailySummaryAsync(1, Arg.Any<CancellationToken>()).Returns(summary);

    // Act
    var result = await _sut.GetClubDailySummaryAsync(1);

    // Assert - ToString() is "[Date] Summary"; date format is culture-dependent
    result.Should().Contain("Summary text");
    result.Should().StartWith("[").And.EndWith("] Summary text");
  }

  // ---- GetClubRecentGamesAsync ----

  [Fact]
  public async Task GetClubRecentGamesAsync_WhenClubNotFound_ReturnsNull()
  {
    // Arrange
    _clubRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Club?)null);

    // Act
    var result = await _sut.GetClubRecentGamesAsync(1);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task GetClubRecentGamesAsync_WhenNoMatches_ReturnsEmptyList()
  {
    // Arrange
    _clubRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Club { Id = 1, Name = "Club" });
    _matchRepo.GetRecentMatchesForClubAsync(1, 5, Arg.Any<CancellationToken>()).Returns(new List<Match>());

    // Act
    var result = await _sut.GetClubRecentGamesAsync(1);

    // Assert
    result.Should().NotBeNull().And.BeEmpty();
  }

  [Fact]
  public async Task GetClubRecentGamesAsync_WhenHomeWin_ReturnsWinWithOpponentAndScore()
  {
    // Arrange
    var clubId = 10;
    _clubRepo.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new Club { Id = clubId, Name = "Home FC" });
    var (match, _, awayClub) = BuildMatchWithClubs(1, clubId, 20, "Home FC", "Away FC", homeGoals: 2, awayGoals: 1);
    _matchRepo.GetRecentMatchesForClubAsync(clubId, 5, Arg.Any<CancellationToken>()).Returns(new List<Match> { match });

    // Act
    var result = await _sut.GetClubRecentGamesAsync(clubId);

    // Assert
    result.Should().HaveCount(1);
    result![0].Result.Should().Be("Win");
    result[0].Opponent.Should().Be("Away FC");
    result[0].Score.Should().Be("2 : 1");
    result[0].MatchId.Should().Be(1);
  }

  [Fact]
  public async Task GetClubRecentGamesAsync_WhenAwayWin_ReturnsWinWithOpponent()
  {
    // Arrange
    var clubId = 20;
    _clubRepo.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new Club { Id = clubId, Name = "Away FC" });
    var (match, homeClub, _) = BuildMatchWithClubs(1, 10, clubId, "Home FC", "Away FC", homeGoals: 0, awayGoals: 2);
    _matchRepo.GetRecentMatchesForClubAsync(clubId, 5, Arg.Any<CancellationToken>()).Returns(new List<Match> { match });

    // Act
    var result = await _sut.GetClubRecentGamesAsync(clubId);

    // Assert
    result.Should().HaveCount(1);
    result![0].Result.Should().Be("Win");
    result[0].Opponent.Should().Be("Home FC");
    result[0].Score.Should().Be("0 : 2");
  }

  [Fact]
  public async Task GetClubRecentGamesAsync_WhenDraw_ReturnsDraw()
  {
    // Arrange
    var clubId = 10;
    _clubRepo.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new Club { Id = clubId, Name = "Home FC" });
    var (match, _, _) = BuildMatchWithClubs(1, clubId, 20, homeGoals: 1, awayGoals: 1);
    _matchRepo.GetRecentMatchesForClubAsync(clubId, 5, Arg.Any<CancellationToken>()).Returns(new List<Match> { match });

    // Act
    var result = await _sut.GetClubRecentGamesAsync(clubId);

    // Assert
    result.Should().HaveCount(1);
    result![0].Result.Should().Be("Draw");
    result[0].Score.Should().Be("1 : 1");
  }

  [Fact]
  public async Task GetClubRecentGamesAsync_WhenGoalsNull_TreatsAsZero()
  {
    // Arrange
    var clubId = 10;
    _clubRepo.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new Club { Id = clubId, Name = "Home FC" });
    var (match, _, _) = BuildMatchWithClubs(1, clubId, 20, homeGoals: null, awayGoals: null);
    _matchRepo.GetRecentMatchesForClubAsync(clubId, 5, Arg.Any<CancellationToken>()).Returns(new List<Match> { match });

    // Act
    var result = await _sut.GetClubRecentGamesAsync(clubId);

    // Assert
    result.Should().HaveCount(1);
    result![0].Score.Should().Be("0 : 0");
    result[0].Result.Should().Be("Draw");
  }

  [Fact]
  public async Task GetClubRecentGamesAsync_WhenMultipleMatches_ReturnsOrderedByDateDescending()
  {
    // Arrange
    var clubId = 10;
    var date1 = DateTime.UtcNow.AddDays(-5);
    var date2 = DateTime.UtcNow.AddDays(-2);
    _clubRepo.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new Club { Id = clubId, Name = "Home FC" });
    var (m1, _, _) = BuildMatchWithClubs(1, clubId, 20, matchDate: date1);
    var (m2, _, _) = BuildMatchWithClubs(2, clubId, 21, matchDate: date2);
    _matchRepo.GetRecentMatchesForClubAsync(clubId, 5, Arg.Any<CancellationToken>()).Returns(new List<Match> { m1, m2 });

    // Act
    var result = await _sut.GetClubRecentGamesAsync(clubId);

    // Assert
    result.Should().HaveCount(2);
    result![0].Date.Should().Be(DateOnly.FromDateTime(date2));
    result[1].Date.Should().Be(DateOnly.FromDateTime(date1));
  }

  // ---- GetClubStatistics ----

  [Fact]
  public async Task GetClubStatistics_WhenStatsNull_ReturnsNull()
  {
    // Arrange
    _clubRepo.GetCurrentClubLeagueStatsAsync(1, Arg.Any<CancellationToken>()).Returns((ClubLeagueStats?)null);

    // Act
    var result = await _sut.GetClubStatistics(1);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task GetClubStatistics_WhenStatsExist_ReturnsSameInstance()
  {
    // Arrange
    var row = new LeagueTableSnapshotRow { Position = 3, Points = 30, Wins = 9, Draws = 3, Losses = 2, GoalsFor = 25, GoalsAgainst = 10, Xg = 28m, XgDiff = -3m, Xga = 12m, XgaDiff = 2m, Xpts = 32m, XptsDiff = -2m };
    var stats = new ClubLeagueStats(row);
    _clubRepo.GetCurrentClubLeagueStatsAsync(1, Arg.Any<CancellationToken>()).Returns(stats);

    // Act
    var result = await _sut.GetClubStatistics(1);

    // Assert
    result.Should().BeSameAs(stats);
    result!.Position.Should().Be(3);
  }

  // ---- GetMatchBettingOddsHistoryAsync ----

  [Fact]
  public async Task GetMatchBettingOddsHistoryAsync_WhenNoSnapshots_ReturnsNull()
  {
    // Arrange
    _bettingRepo.GetBettingOddsSnapshotsForMatchAsync(MatchId, Arg.Any<CancellationToken>()).Returns(new List<BettingOddsSnapshot>());

    // Act
    var result = await _sut.GetMatchBettingOddsHistoryAsync();

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task GetMatchBettingOddsHistoryAsync_WhenOnlyNonWhitelistedEventTypes_ReturnsEmptySections()
  {
    // Arrange - FirstTeamToScore and PlayerOrSubToScore are not in whitelist
    var ev = new BookmakerEvent { Title = "First", Options = new List<EventOption> { new() { Label = "Home", Odds = 1.9 } } };
    var eventJson = JsonSerializer.Serialize(ev, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    var snapshot = BuildSnapshot(DateTime.UtcNow, (int)BettingEventType.FirstTeamToScore, "FirstTeamToScore", eventJson);
    _bettingRepo.GetBettingOddsSnapshotsForMatchAsync(MatchId, Arg.Any<CancellationToken>()).Returns(new List<BettingOddsSnapshot> { snapshot });

    // Act
    var result = await _sut.GetMatchBettingOddsHistoryAsync();

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
  }

  [Fact]
  public async Task GetMatchBettingOddsHistoryAsync_WhenWhitelistedEventType_ReturnsMarketPriceHistory()
  {
    // Arrange
    var ev = new BookmakerEvent
    {
      Title = "Match Result",
      Options = new List<EventOption> { new() { Label = "Home", Odds = 1.85 }, new() { Label = "Draw", Odds = 3.5 } }
    };
    var eventJson = JsonSerializer.Serialize(ev, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    var snapshot = BuildSnapshot(DateTime.UtcNow, (int)BettingEventType.MatchResult, "Match Result", eventJson);
    _bettingRepo.GetBettingOddsSnapshotsForMatchAsync(MatchId, Arg.Any<CancellationToken>()).Returns(new List<BettingOddsSnapshot> { snapshot });

    // Act
    var result = await _sut.GetMatchBettingOddsHistoryAsync();

    // Assert
    result.Should().HaveCount(1);
    result![0].MarketKey.Should().Be("Match Result");
    result[0].MarketDisplayName.Should().Be("Match Result");
    result[0].Outcomes.Should().HaveCount(2);
    result[0].Outcomes.Select(o => o.OutcomeName).Should().Contain("Home").And.Contain("Draw");
    result[0].Outcomes[0].Timeline.Should().HaveCount(1);
    result[0].Outcomes[0].Timeline[0].EffectiveTo.Should().BeNull();
  }

  [Fact]
  public async Task GetMatchBettingOddsHistoryAsync_WhenEventJsonDeserializesToNull_SkipsRow()
  {
    // Arrange - valid whitelisted row + row with JSON "null" so Deserialize returns null
    var validEv = new BookmakerEvent { Title = "1X2", Options = new List<EventOption> { new() { Label = "1", Odds = 2.0 } } };
    var validJson = JsonSerializer.Serialize(validEv, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    var snapshot = new BettingOddsSnapshot { MatchId = MatchId, SnapshotTime = DateTime.UtcNow };
    snapshot.Rows.Add(new BettingOddsSnapshotRow
    {
      EventTypeId = (int)BettingEventType.MatchResult,
      EventJson = validJson,
      EventTypeEntity = new BettingEventTypeEntity { Name = "Match Result" }
    });
    snapshot.Rows.Add(new BettingOddsSnapshotRow
    {
      EventTypeId = (int)BettingEventType.MatchResult,
      EventJson = "null",
      EventTypeEntity = new BettingEventTypeEntity { Name = "Match Result" }
    });
    _bettingRepo.GetBettingOddsSnapshotsForMatchAsync(MatchId, Arg.Any<CancellationToken>()).Returns(new List<BettingOddsSnapshot> { snapshot });

    // Act
    var result = await _sut.GetMatchBettingOddsHistoryAsync();

    // Assert - null row skipped, valid row produces one outcome
    result.Should().HaveCount(1);
    result![0].Outcomes.Should().HaveCount(1);
  }

  [Fact]
  public async Task GetMatchBettingOddsHistoryAsync_WhenMultipleSnapshotsDifferentOdds_ProducesSegmentsWithLastEffectiveToNull()
  {
    // Arrange
    var t1 = DateTime.UtcNow.AddHours(-2);
    var t2 = DateTime.UtcNow.AddHours(-1);
    var ev1 = new BookmakerEvent { Title = "OU", Options = new List<EventOption> { new() { Label = "Over 2.5", Odds = 1.9 } } };
    var ev2 = new BookmakerEvent { Title = "OU", Options = new List<EventOption> { new() { Label = "Over 2.5", Odds = 2.0 } } };
    var snapshot1 = BuildSnapshot(t1, (int)BettingEventType.OverUnderGoals, "Over/Under", JsonSerializer.Serialize(ev1, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    var snapshot2 = BuildSnapshot(t2, (int)BettingEventType.OverUnderGoals, "Over/Under", JsonSerializer.Serialize(ev2, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    _bettingRepo.GetBettingOddsSnapshotsForMatchAsync(MatchId, Arg.Any<CancellationToken>()).Returns(new List<BettingOddsSnapshot> { snapshot1, snapshot2 });

    // Act
    var result = await _sut.GetMatchBettingOddsHistoryAsync();

    // Assert
    result.Should().HaveCount(1);
    var timeline = result![0].Outcomes[0].Timeline;
    timeline.Should().HaveCount(2);
    timeline[0].Price.Should().Be(1.9);
    timeline[0].EffectiveTo.Should().Be(t2);
    timeline[1].Price.Should().Be(2.0);
    timeline[1].EffectiveTo.Should().BeNull();
  }

  [Fact]
  public async Task GetMatchBettingOddsHistoryAsync_WhenOptionInFirstSnapshotOnly_StillProducesOutcomeWithSegments()
  {
    // Arrange - second snapshot has different option set (e.g. only "Under")
    var t1 = DateTime.UtcNow.AddHours(-2);
    var t2 = DateTime.UtcNow.AddHours(-1);
    var ev1 = new BookmakerEvent { Title = "OU", Options = new List<EventOption> { new() { Label = "Over 2.5", Odds = 1.9 }, new() { Label = "Under 2.5", Odds = 1.95 } } };
    var ev2 = new BookmakerEvent { Title = "OU", Options = new List<EventOption> { new() { Label = "Under 2.5", Odds = 2.0 } } };
    var snapshot1 = BuildSnapshot(t1, (int)BettingEventType.OverUnderGoals, "Over/Under", JsonSerializer.Serialize(ev1, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    var snapshot2 = BuildSnapshot(t2, (int)BettingEventType.OverUnderGoals, "Over/Under", JsonSerializer.Serialize(ev2, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    _bettingRepo.GetBettingOddsSnapshotsForMatchAsync(MatchId, Arg.Any<CancellationToken>()).Returns(new List<BettingOddsSnapshot> { snapshot1, snapshot2 });

    // Act
    var result = await _sut.GetMatchBettingOddsHistoryAsync();

    // Assert
    result.Should().HaveCount(1);
    var overOutcome = result![0].Outcomes.First(o => o.OutcomeName == "Over 2.5");
    overOutcome.Timeline.Should().HaveCount(1);
    overOutcome.Timeline[0].EffectiveTo.Should().BeNull();
    var underOutcome = result[0].Outcomes.First(o => o.OutcomeName == "Under 2.5");
    underOutcome.Timeline.Should().HaveCount(2);
  }

  [Fact]
  public async Task GetMatchBettingOddsHistoryAsync_WhenEventTypeEntityNameSet_ReflectsInMarketKey()
  {
    // Arrange
    var ev = new BookmakerEvent { Title = "1X2", Options = new List<EventOption> { new() { Label = "1", Odds = 1.8 } } };
    var eventJson = JsonSerializer.Serialize(ev, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    var snapshot = BuildSnapshot(DateTime.UtcNow, (int)BettingEventType.MatchResult, "Match Result", eventJson);
    _bettingRepo.GetBettingOddsSnapshotsForMatchAsync(MatchId, Arg.Any<CancellationToken>()).Returns(new List<BettingOddsSnapshot> { snapshot });

    // Act
    var result = await _sut.GetMatchBettingOddsHistoryAsync();

    // Assert
    result![0].MarketKey.Should().Be("Match Result");
  }

  // ---- GetClubRollingPerformanceAsync ----

  [Fact]
  public async Task GetClubRollingPerformanceAsync_WhenClubNotFound_ReturnsNull()
  {
    // Arrange
    _clubRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Club?)null);

    // Act
    var result = await _sut.GetClubRollingPerformanceAsync(1);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task GetClubRollingPerformanceAsync_WhenNoRecentMatches_ReturnsEmptyResult()
  {
    // Arrange
    _clubRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Club { Id = 1, Name = "Club" });
    _matchRepo.GetRecentMatchesForClubAsync(1, 5, Arg.Any<CancellationToken>()).Returns(new List<Match>());

    // Act
    var result = await _sut.GetClubRollingPerformanceAsync(1);

    // Assert
    result.Should().NotBeNull();
    result!.TopPlayers.Should().BeEmpty();
    result.RecentTeamRatings.Should().BeEmpty();
    result.Formations.Should().BeEmpty();
    result.AvgTeamRating.Should().Be(0);
  }

  [Fact]
  public async Task GetClubRollingPerformanceAsync_WhenMatchDetailsNull_SkipsMatchAndDoesNotThrow()
  {
    // Arrange
    var clubId = 10;
    _clubRepo.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new Club { Id = clubId, Name = "Club" });
    var (match, _, _) = BuildMatchWithClubs(1, clubId, 20);
    _matchRepo.GetRecentMatchesForClubAsync(clubId, 5, Arg.Any<CancellationToken>()).Returns(new List<Match> { match });
    _matchRepo.GetMatchDetailsByMatchIdAsync(1, Arg.Any<CancellationToken>()).Returns((MatchDetails?)null);

    // Act
    var result = await _sut.GetClubRollingPerformanceAsync(clubId);

    // Assert
    result.Should().NotBeNull();
    result!.TopPlayers.Should().BeEmpty();
    result.RecentTeamRatings.Should().BeEmpty();
    result.Formations.Should().BeEmpty();
  }

  [Fact]
  public async Task GetClubRollingPerformanceAsync_WhenFotmobDetailsJsonEmpty_SkipsMatch()
  {
    // Arrange
    var clubId = 10;
    _clubRepo.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new Club { Id = clubId, Name = "Club" });
    var (match, _, _) = BuildMatchWithClubs(1, clubId, 20);
    _matchRepo.GetRecentMatchesForClubAsync(clubId, 5, Arg.Any<CancellationToken>()).Returns(new List<Match> { match });
    var details = new MatchDetails { MatchId = 1, FotmobDetailsJson = null };
    _matchRepo.GetMatchDetailsByMatchIdAsync(1, Arg.Any<CancellationToken>()).Returns(details);

    // Act
    var result = await _sut.GetClubRollingPerformanceAsync(clubId);

    // Assert
    result!.TopPlayers.Should().BeEmpty();
    result.RecentTeamRatings.Should().BeEmpty();
  }

  [Fact]
  public async Task GetClubRollingPerformanceAsync_WhenLineupNullForClub_SkipsMatch()
  {
    // Arrange - club is away, payload has only HomeLineup
    var clubId = 20;
    _clubRepo.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new Club { Id = clubId, Name = "Away" });
    var (match, _, _) = BuildMatchWithClubs(1, 10, clubId);
    _matchRepo.GetRecentMatchesForClubAsync(clubId, 5, Arg.Any<CancellationToken>()).Returns(new List<Match> { match });
    var payload = new FotmobDetailsPayload(
      HomeLineup: new FotmobTeamLineup { TeamName = "Home", Formation = "4-3-3", TeamRating = 7.0, Players = [] },
      AwayLineup: null,
      Statistics: null,
      Players: null);
    var details = new MatchDetails { MatchId = 1, FotmobDetailsJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)) };
    _matchRepo.GetMatchDetailsByMatchIdAsync(1, Arg.Any<CancellationToken>()).Returns(details);

    // Act
    var result = await _sut.GetClubRollingPerformanceAsync(clubId);

    // Assert
    result!.TopPlayers.Should().BeEmpty();
    result.RecentTeamRatings.Should().BeEmpty();
    result.Formations.Should().BeEmpty();
  }

  [Fact]
  public async Task GetClubRollingPerformanceAsync_WhenPlayersNull_StillAddsTeamRatingAndFormation()
  {
    // Arrange
    var clubId = 10;
    _clubRepo.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new Club { Id = clubId, Name = "Home" });
    var (match, _, _) = BuildMatchWithClubs(1, clubId, 20);
    _matchRepo.GetRecentMatchesForClubAsync(clubId, 5, Arg.Any<CancellationToken>()).Returns(new List<Match> { match });
    var lineup = new FotmobTeamLineup { TeamName = "Home", Formation = "4-4-2", TeamRating = 6.5, Players = null! };
    var payload = new FotmobDetailsPayload(HomeLineup: lineup, AwayLineup: null, Statistics: null, Players: null);
    var details = new MatchDetails { MatchId = 1, FotmobDetailsJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)) };
    _matchRepo.GetMatchDetailsByMatchIdAsync(1, Arg.Any<CancellationToken>()).Returns(details);

    // Act
    var result = await _sut.GetClubRollingPerformanceAsync(clubId);

    // Assert
    result!.TopPlayers.Should().BeEmpty();
    result.RecentTeamRatings.Should().ContainSingle().Which.Should().Be(6.5);
    result.Formations.Should().ContainSingle().Which.Should().Be("4-4-2");
    result.AvgTeamRating.Should().Be(6.5);
  }

  [Fact]
  public async Task GetClubRollingPerformanceAsync_WhenPlayerNameNullOrBlank_SkipsPlayer()
  {
    // Arrange - one player with name, one with null name, one with whitespace
    var clubId = 10;
    _clubRepo.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new Club { Id = clubId, Name = "Home" });
    var (match, _, _) = BuildMatchWithClubs(1, clubId, 20);
    _matchRepo.GetRecentMatchesForClubAsync(clubId, 5, Arg.Any<CancellationToken>()).Returns(new List<Match> { match });
    var players = new List<FotmobLineupPlayer>
    {
      new() { Name = "Valid", Rating = 7.5 },
      new() { Name = null!, Rating = 6.0 },
      new() { Name = "  ", Rating = 6.5 }
    };
    var lineup = new FotmobTeamLineup { TeamName = "Home", Formation = "4-3-3", TeamRating = 7.0, Players = players };
    var payload = new FotmobDetailsPayload(HomeLineup: lineup, AwayLineup: null, Statistics: null, Players: null);
    var details = new MatchDetails { MatchId = 1, FotmobDetailsJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)) };
    _matchRepo.GetMatchDetailsByMatchIdAsync(1, Arg.Any<CancellationToken>()).Returns(details);

    // Act
    var result = await _sut.GetClubRollingPerformanceAsync(clubId);

    // Assert
    result!.TopPlayers.Should().ContainSingle().Which.Player.Should().Be("Valid");
    result.TopPlayers[0].RecentRatings.Should().ContainSingle().Which.Should().Be(7.5);
  }

  [Fact]
  public async Task GetClubRollingPerformanceAsync_WhenPlayerRatingNull_SkipsPlayer()
  {
    // Arrange
    var clubId = 10;
    _clubRepo.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new Club { Id = clubId, Name = "Home" });
    var (match, _, _) = BuildMatchWithClubs(1, clubId, 20);
    _matchRepo.GetRecentMatchesForClubAsync(clubId, 5, Arg.Any<CancellationToken>()).Returns(new List<Match> { match });
    var players = new List<FotmobLineupPlayer> { new() { Name = "NoRating", Rating = null } };
    var lineup = new FotmobTeamLineup { TeamName = "Home", Formation = "4-3-3", TeamRating = 7.0, Players = players };
    var payload = new FotmobDetailsPayload(HomeLineup: lineup, AwayLineup: null, Statistics: null, Players: null);
    var details = new MatchDetails { MatchId = 1, FotmobDetailsJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)) };
    _matchRepo.GetMatchDetailsByMatchIdAsync(1, Arg.Any<CancellationToken>()).Returns(details);

    // Act
    var result = await _sut.GetClubRollingPerformanceAsync(clubId);

    // Assert
    result!.TopPlayers.Should().BeEmpty();
  }

  [Fact]
  public async Task GetClubRollingPerformanceAsync_WhenFormationNull_StoresEmptyString()
  {
    // Arrange
    var clubId = 10;
    _clubRepo.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new Club { Id = clubId, Name = "Home" });
    var (match, _, _) = BuildMatchWithClubs(1, clubId, 20);
    _matchRepo.GetRecentMatchesForClubAsync(clubId, 5, Arg.Any<CancellationToken>()).Returns(new List<Match> { match });
    var lineup = new FotmobTeamLineup { TeamName = "Home", Formation = null, TeamRating = 7.0, Players = [] };
    var payload = new FotmobDetailsPayload(HomeLineup: lineup, AwayLineup: null, Statistics: null, Players: null);
    var details = new MatchDetails { MatchId = 1, FotmobDetailsJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)) };
    _matchRepo.GetMatchDetailsByMatchIdAsync(1, Arg.Any<CancellationToken>()).Returns(details);

    // Act
    var result = await _sut.GetClubRollingPerformanceAsync(clubId);

    // Assert
    result!.Formations.Should().ContainSingle().Which.Should().BeEmpty();
  }

  [Fact]
  public async Task GetClubRollingPerformanceAsync_WhenValidData_ReturnsTopPlayersAndRatingsOrderedByAvgDesc()
  {
    // Arrange
    var clubId = 10;
    _clubRepo.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new Club { Id = clubId, Name = "Home" });
    var (match, _, _) = BuildMatchWithClubs(1, clubId, 20);
    _matchRepo.GetRecentMatchesForClubAsync(clubId, 5, Arg.Any<CancellationToken>()).Returns(new List<Match> { match });
    var players = new List<FotmobLineupPlayer>
    {
      new() { Name = "Low", Rating = 6.0 },
      new() { Name = "High", Rating = 8.0 }
    };
    var lineup = new FotmobTeamLineup { TeamName = "Home", Formation = "4-3-3", TeamRating = 7.2, Players = players };
    var payload = new FotmobDetailsPayload(HomeLineup: lineup, AwayLineup: null, Statistics: null, Players: null);
    var details = new MatchDetails { MatchId = 1, FotmobDetailsJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)) };
    _matchRepo.GetMatchDetailsByMatchIdAsync(1, Arg.Any<CancellationToken>()).Returns(details);

    // Act
    var result = await _sut.GetClubRollingPerformanceAsync(clubId);

    // Assert
    result!.TopPlayers.Should().HaveCount(2);
    result.TopPlayers[0].Player.Should().Be("High");
    result.TopPlayers[0].AvgRating.Should().Be(8.0);
    result.TopPlayers[1].Player.Should().Be("Low");
    result.TopPlayers[1].AvgRating.Should().Be(6.0);
    result.RecentTeamRatings.Should().ContainSingle().Which.Should().Be(7.2);
    result.AvgTeamRating.Should().Be(7.2);
    result.Formations.Should().ContainSingle().Which.Should().Be("4-3-3");
  }
}
