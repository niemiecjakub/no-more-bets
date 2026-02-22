//using FluentAssertions;
//using MediatR;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Abstractions;
//using Microsoft.Extensions.Options;
//using Moq;
//using NoMoreBets.Domain.Enums;
//using NoMoreBets.Features.Betclic.Model;
//using NoMoreBets.Features.Betclic.GetBetclicMatchEvents;
//using NoMoreBets.Features.Betclic.GetBetclicUpcomingGames;
//using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable;
//using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable.Dtos;
//using NoMoreBets.Features.MatchAnalysis.MatchMatcher;
//using NoMoreBets.Features.MatchAnalysis.Model;
//using NoMoreBets.Features.MatchAnalysis.Options;
//using NoMoreBets.Features.MatchAnalysis.RunMatchAnalysis;
//using NoMoreBets.Features.Rotowire.GetRotowireLineups;
//using NoMoreBets.Features.Rotowire.Model;
//using NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;
//using NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;
//using NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;
//using NoMoreBets.Features.SoccerData.Model;

//namespace NoMoreBets.Tests.Features.MatchAnalysis;

//public class RunMatchAnalysisHandlerTests
//{
//    private readonly Mock<IMediator> _mediatorMock;
//    private readonly Mock<IMatchMatcher> _matchMatcherMock;
//    private readonly IOptions<MatchAnalysisOptions> _options;
//    private readonly ILogger<RunMatchAnalysisHandler> _logger;
//    private readonly Mock<NoMoreBets.Features.MatchAnalysis.Persistence.IMatchAnalysisPersistence>? _persistenceMock;

//    public RunMatchAnalysisHandlerTests()
//    {
//        _mediatorMock = new Mock<IMediator>();
//        _matchMatcherMock = new Mock<IMatchMatcher>();
//        _options = Options.CreateUpcomming(new MatchAnalysisOptions { SoccerdataLeagueId = 228, OutputDirectory = "" });
//        _logger = NullLogger<RunMatchAnalysisHandler>.Instance;
//        _persistenceMock = new Mock<NoMoreBets.Features.MatchAnalysis.Persistence.IMatchAnalysisPersistence>();
//    }

//    [Fact]
//    public async Task Handle_WithOneUpcomingGame_ReturnsOneMatchAnalysis()
//    {
//        // Arrange
//        var lineup = new GameLineup
//        {
//            Date = new DateTime(2026, 1, 15),
//            Time = "15:00",
//            HomeTeam = new TeamLineup
//            {
//                TeamName = "Arsenal",
//                LineupType = LineupType.Predicted,
//                Players = [new PlayerInLineup(FootballPosition.GK, "Raya")],
//                Injuries = []
//            },
//            AwayTeam = new TeamLineup
//            {
//                TeamName = "Chelsea",
//                LineupType = LineupType.Predicted,
//                Players = [new PlayerInLineup(FootballPosition.GK, "Sanchez")],
//                Injuries = []
//            }
//        };
//        var lineups = new List<GameLineup> { lineup };
//        var lineupIndex = new Dictionary<TeamKey, GameLineup> { [new TeamKey("Arsenal", "Chelsea")] = lineup };

//        var upcomingMatch = new UpcomingMatchPreview
//        {
//            Id = 100,
//            Date = "15/01/2026",
//            Time = "15:00",
//            ExcitementRating = 7.5,
//            Teams = new Teams
//            {
//                Home = new NoMoreBets.Features.SoccerData.Model.TeamInfo { Id = 1, Name = "Arsenal" },
//                Away = new NoMoreBets.Features.SoccerData.Model.TeamInfo { Id = 2, Name = "Chelsea" }
//            }
//        };
//        var leagues = new List<LeagueMatchPreviews>
//        {
//            new() { SoccerdataLeagueId = 228, LeagueName = "Premier League", MatchPreviews = [upcomingMatch] }
//        };

//        var homeClub = new ClubDto(1, "Arsenal", "ARS", 42, "", 20, 15, 3, 2, 45, 20, "+25", 48, new[] { "Win", "Win", "Win", "Draw", "Win" }, null, null, null);
//        var awayClub = new ClubDto(2, "Chelsea", "CHE", 43, "", 20, 12, 4, 4, 38, 25, "+13", 40, new[] { "Win", "Loss", "Win", "Draw", "Win" }, null, null, null);
//        var clubs = new List<ClubDto> { homeClub, awayClub };

//        var game = new UpcomingGame
//        {
//            Date = new DateTime(2026, 1, 15),
//            Time = "15:00",
//            HomeTeam = "Arsenal",
//            AwayTeam = "Chelsea",
//            Url = "https://betclic.example/match"
//        };
//        var games = new List<UpcomingGame> { game };

//        var events = new List<BookmakerEvent>
//        {
//            new() { Title = "Match Winner", Options = [new EventOption { Label = "Arsenal", Odds = 2.1 }] }
//        };

//        var headToHead = new HeadToHead
//        {
//            Team1 = new NoMoreBets.Features.SoccerData.Model.TeamInfo { Id = 1, Name = "Arsenal" },
//            Team2 = new NoMoreBets.Features.SoccerData.Model.TeamInfo { Id = 2, Name = "Chelsea" },
//            Stats = new HeadToHeadStats
//            {
//                Overall = new NoMoreBets.Features.SoccerData.Model.OverallStats { OverallGamesPlayed = 10, OverallTeam1Wins = 4, OverallTeam2Wins = 3, OverallDraws = 3, OverallTeam1Scored = 12, OverallTeam2Scored = 10 },
//                Team1AtHome = new NoMoreBets.Features.SoccerData.Model.Team1AtHomeStats { Team1GamesPlayedAtHome = 5, Team1WinsAtHome = 2, Team1LossesAtHome = 1, Team1DrawsAtHome = 2, Team1ScoredAtHome = 6, Team1ConcededAtHome = 4 },
//                Team2AtHome = new NoMoreBets.Features.SoccerData.Model.Team2AtHomeStats { Team2GamesPlayedAtHome = 5, Team2WinsAtHome = 2, Team2LossesAtHome = 1, Team2DrawsAtHome = 2, Team2ScoredAtHome = 5, Team2ConcededAtHome = 5 }
//            }
//        };

//        var matchPreview = new MatchPreview
//        {
//            Id = 100,
//            Date = "15/01/2026",
//            Time = "15:00",
//            Country = new CountryInfo(),
//            League = new LeagueInfo(),
//            Stage = new StageInfo(),
//            Teams = new Teams { Home = new NoMoreBets.Features.SoccerData.Model.TeamInfo { Id = 1, Name = "Arsenal" }, Away = new NoMoreBets.Features.SoccerData.Model.TeamInfo { Id = 2, Name = "Chelsea" } },
//            MatchData = new MatchData
//            {
//                ExcitementRating = 7.5,
//                Weather = new Weather { Description = "Clear", TempC = 12, TempF = 54 },
//                Prediction = new NoMoreBets.Features.SoccerData.Model.Prediction { Type = "match_winner", Choice = "home" }
//            },
//            PreviewContent = []
//        };

//        _mediatorMock
//            .Setup(m => m.Send(It.IsAny<GetRotowireLineupsQuery>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(lineups);
//        _mediatorMock
//            .Setup(m => m.Send(It.IsAny<GetSoccerDataMatchPreviewsUpcomingQuery>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(leagues);
//        _mediatorMock
//            .Setup(m => m.Send(It.IsAny<GetFotmobLeagueTableQuery>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(clubs);
//        _mediatorMock
//            .Setup(m => m.Send(It.IsAny<GetBetclicUpcomingGamesQuery>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(games);
//        _mediatorMock
//            .Setup(m => m.Send(It.IsAny<GetBetclicMatchEventsQuery>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(events);
//        _mediatorMock
//            .Setup(m => m.Send(It.IsAny<GetSoccerDataHeadToHeadQuery>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(headToHead);
//        _mediatorMock
//            .Setup(m => m.Send(It.IsAny<GetSoccerDataMatchPreviewQuery>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(matchPreview);

//        _matchMatcherMock.Setup(m => m.BuildLineupIndex(It.IsAny<IReadOnlyList<GameLineup>>())).Returns(lineupIndex);
//        _matchMatcherMock.Setup(m => m.FindLineup(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<TeamKey, GameLineup>>())).Returns(lineup);
//        _matchMatcherMock.Setup(m => m.FindSoccerDataMatch(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<LeagueMatchPreviews>>())).Returns(upcomingMatch);
//        _matchMatcherMock.Setup(m => m.FindFotmobClub("Arsenal", It.IsAny<IReadOnlyList<ClubDto>>())).Returns(homeClub);
//        _matchMatcherMock.Setup(m => m.FindFotmobClub("Chelsea", It.IsAny<IReadOnlyList<ClubDto>>())).Returns(awayClub);

//        var handler = new RunMatchAnalysisHandler(
//            _mediatorMock.Object,
//            _matchMatcherMock.Object,
//            _options,
//            _logger,
//            _persistenceMock?.Object);

//        // Act
//        var result = await handler.Handle(new RunMatchAnalysisQuery(228), CancellationToken.None);

//        // Assert
//        result.Should().HaveCount(1);
//        var analysis = result[0];
//        analysis.Game.Should().Be("Arsenal vs Chelsea");
//        analysis.Date.Should().Be(new DateTime(2026, 1, 15, 15, 0, 0));
//        analysis.HomeTeam.Name.Should().Be("Arsenal");
//        analysis.AwayTeam.Name.Should().Be("Chelsea");
//        analysis.HomeTeam.Lineup.Should().NotBeNull();
//        analysis.AwayTeam.Lineup.Should().NotBeNull();
//        analysis.HomeTeam.LeagueStatistics.Should().NotBeNull();
//        analysis.HomeTeam.LeagueStatistics!.Points.Should().Be(48);
//        analysis.AwayTeam.LeagueStatistics.Should().NotBeNull();
//        analysis.AwayTeam.LeagueStatistics!.Points.Should().Be(40);
//        analysis.HeadToHead.Should().NotBeNull();
//        analysis.HeadToHead!.Home.Name.Should().Be("Arsenal");
//        analysis.HeadToHead.Away.Name.Should().Be("Chelsea");
//        analysis.Preview.Should().NotBeNull().And.BeEmpty();
//        analysis.Betting.Should().NotBeNull().And.HaveCount(1);
//        analysis.Betting![0].Title.Should().Be("Match Winner");

//        _persistenceMock?.Verify(p => p.SaveResultsAsync(It.IsAny<IReadOnlyList<NoMoreBets.Features.MatchAnalysis.Model.MatchAnalysis>>(), It.IsAny<CancellationToken>()), Times.Once);
//    }
//}
