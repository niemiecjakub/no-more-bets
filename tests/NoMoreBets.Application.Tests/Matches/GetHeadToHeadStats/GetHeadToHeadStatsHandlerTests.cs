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
  public async Task Handle_WhenHomeClubIsStoredAsTeam2_MapsTeamAMetricsFromTeam2Stats()
  {
    // Arrange
    const int matchId = 7;
    var match = new Match
    {
      Id = matchId,
      HomeClubId = 2,
      AwayClubId = 1,
      HomeClub = new ClubEntity { Id = 2, Name = "Arsenal" },
      AwayClub = new ClubEntity { Id = 1, Name = "Liverpool" }
    };
    var h2hJson = """
                  {"team1":{"id":1,"name":"Liverpool"},"team2":{"id":2,"name":"Arsenal"},"stats":{"overall":{"overallGamesPlayed":8,"overallTeam1Wins":2,"overallTeam2Wins":4,"overallDraws":2,"overallTeam1Scored":8,"overallTeam2Scored":11},"team1AtHome":{"team1GamesPlayedAtHome":4,"team1WinsAtHome":1,"team1LossesAtHome":2,"team1DrawsAtHome":1,"team1ScoredAtHome":4,"team1ConcededAtHome":6},"team2AtHome":{"team2GamesPlayedAtHome":4,"team2WinsAtHome":2,"team2LossesAtHome":1,"team2DrawsAtHome":1,"team2ScoredAtHome":5,"team2ConcededAtHome":2}}}
                  """;
    var head2Head = new Head2Head { Team1Id = 1, Team2Id = 2, Head2HeadJson = h2hJson };
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
}
