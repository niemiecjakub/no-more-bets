using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetHeadToHeadStats;
using NoMoreBets.Domain.Matches;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Matches.GetHeadToHeadStats;

public class GetHeadToHeadStatsHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matchRepository = Substitute.For<IMatchRepository>();
  private readonly GetHeadToHeadStatsHandler _sut;

  public GetHeadToHeadStatsHandlerTests()
  {
    _unitOfWork.Matches.Returns(_matchRepository);
    _sut = new GetHeadToHeadStatsHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenHomeClubIsJsonTeam2_MapsTeamAMetricsFromTeam2Stats()
  {
    // Arrange: entity Team1Id/Team2Id are normalized DB ids; JSON team ids are Soccerdata ids.
    // Home (Arsenal) is JSON team2 even though its DB id is larger than away's.
    const int matchId = 7;
    var match = new Match
    {
      Id = matchId,
      HomeClubId = 50,
      AwayClubId = 10,
      HomeClub = new ClubEntity { Id = 50, SoccerdataId = 2899, Name = "Arsenal" },
      AwayClub = new ClubEntity { Id = 10, SoccerdataId = 3183, Name = "Liverpool" }
    };
    var h2hJson = """
                  {"team1":{"id":3183,"name":"Liverpool"},"team2":{"id":2899,"name":"Arsenal"},"stats":{"overall":{"overallGamesPlayed":8,"overallTeam1Wins":2,"overallTeam2Wins":4,"overallDraws":2,"overallTeam1Scored":8,"overallTeam2Scored":11},"team1AtHome":{"team1GamesPlayedAtHome":4,"team1WinsAtHome":1,"team1LossesAtHome":2,"team1DrawsAtHome":1,"team1ScoredAtHome":4,"team1ConcededAtHome":6},"team2AtHome":{"team2GamesPlayedAtHome":4,"team2WinsAtHome":2,"team2LossesAtHome":1,"team2DrawsAtHome":1,"team2ScoredAtHome":5,"team2ConcededAtHome":2}}}
                  """;
    var head2Head = new Head2Head { Team1Id = 10, Team2Id = 50, Head2HeadJson = h2hJson };
    _matchRepository.GetMatchByIdAsync(matchId, Arg.Any<CancellationToken>()).Returns(match);
    _matchRepository.GetHeadToHead(match.HomeClubId, match.AwayClubId).Returns(head2Head);

    // Act
    var result = await _sut.Handle(new GetHeadToHeadStatsQuery(matchId), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.TeamA.Name.Should().Be("Arsenal");
    result.TeamA.TotalWins.Should().Be(4);
    result.TeamB.Name.Should().Be("Liverpool");
    result.TeamB.TotalWins.Should().Be(2);
  }

  [Fact]
  public async Task Handle_WhenHomeClubIsJsonTeam1_MapsTeamAMetricsFromTeam1Stats()
  {
    // Arrange: home Soccerdata id matches JSON team1; entity Team1Id is still the smaller DB id (away).
    const int matchId = 8;
    var match = new Match
    {
      Id = matchId,
      HomeClubId = 50,
      AwayClubId = 10,
      HomeClub = new ClubEntity { Id = 50, SoccerdataId = 3183, Name = "Pogon Szczecin" },
      AwayClub = new ClubEntity { Id = 10, SoccerdataId = 2899, Name = "Legia Warsaw" }
    };
    var h2hJson = """
                  {"team1":{"id":3183,"name":"Pogon Szczecin"},"team2":{"id":2899,"name":"Legia Warsaw"},"stats":{"overall":{"overallGamesPlayed":54,"overallTeam1Wins":10,"overallTeam2Wins":32,"overallDraws":12,"overallTeam1Scored":51,"overallTeam2Scored":104},"team1AtHome":{"team1GamesPlayedAtHome":27,"team1WinsAtHome":7,"team1LossesAtHome":13,"team1DrawsAtHome":7,"team1ScoredAtHome":30,"team1ConcededAtHome":45},"team2AtHome":{"team2GamesPlayedAtHome":27,"team2WinsAtHome":19,"team2LossesAtHome":3,"team2DrawsAtHome":5,"team2ScoredAtHome":59,"team2ConcededAtHome":21}}}
                  """;
    var head2Head = new Head2Head { Team1Id = 10, Team2Id = 50, Head2HeadJson = h2hJson };
    _matchRepository.GetMatchByIdAsync(matchId, Arg.Any<CancellationToken>()).Returns(match);
    _matchRepository.GetHeadToHead(match.HomeClubId, match.AwayClubId).Returns(head2Head);

    // Act
    var result = await _sut.Handle(new GetHeadToHeadStatsQuery(matchId), CancellationToken.None);

    // Assert — old entity.Team1Id check would have swapped these (home DB id > away)
    result.Should().NotBeNull();
    result!.TeamA.Name.Should().Be("Pogon Szczecin");
    result.TeamA.TotalWins.Should().Be(10);
    result.TeamA.HomeWins.Should().Be(7);
    result.TeamB.Name.Should().Be("Legia Warsaw");
    result.TeamB.TotalWins.Should().Be(32);
    result.TeamB.HomeWins.Should().Be(19);
  }
}
