using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Clubs.GetClubRollingPerformance;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Club.GetClubRollingPerformance;

public class GetClubRollingPerformanceHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IClubRepository _clubRepository = Substitute.For<IClubRepository>();
  private readonly IMatchRepository _matchRepository = Substitute.For<IMatchRepository>();
  private readonly GetClubRollingPerformanceHandler _sut;

  public GetClubRollingPerformanceHandlerTests()
  {
    _unitOfWork.Clubs.Returns(_clubRepository);
    _unitOfWork.Matches.Returns(_matchRepository);
    _sut = new GetClubRollingPerformanceHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenMultipleGames_ReturnsRatingsOrderedByDateAndTopPlayersByAverage()
  {
    // Arrange
    const int clubId = 5;
    _clubRepository.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new ClubEntity { Id = clubId, Name = "Club A" });

    var older = new Match { Id = 100, HomeClubId = clubId, MatchDate = DateTime.UtcNow.AddDays(-4) };
    var newer = new Match { Id = 101, HomeClubId = clubId, MatchDate = DateTime.UtcNow.AddDays(-1) };
    _matchRepository.GetRecentMatchesForClubAsync(clubId, 5, null, Arg.Any<CancellationToken>())
      .Returns(new List<Match> { newer, older });

    _matchRepository.GetMatchDetailsByMatchIdAsync(older.Id, Arg.Any<CancellationToken>())
      .Returns(new MatchDetails { MatchId = older.Id, FotmobDetailsJson = SerializePayload(6.5, "4-4-2", ("Low", 6.0), ("High", 8.0)) });
    _matchRepository.GetMatchDetailsByMatchIdAsync(newer.Id, Arg.Any<CancellationToken>())
      .Returns(new MatchDetails { MatchId = newer.Id, FotmobDetailsJson = SerializePayload(7.5, "4-3-3", ("Low", 7.0), ("High", 9.0)) });

    // Act
    var result = await _sut.Handle(new GetClubRollingPerformanceQuery(clubId), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.RecentTeamRatings.Should().Equal(6.5, 7.5);
    result.Formations.Should().Equal("4-4-2", "4-3-3");
    result.TopPlayers[0].Player.Should().Be("High");
    result.TopPlayers[0].AvgRating.Should().Be(8.5);
    result.TopPlayers[1].Player.Should().Be("Low");
    result.TopPlayers[1].AvgRating.Should().Be(6.5);
  }

  [Fact]
  public async Task Handle_WithDate_PassesDateToGetRecentMatches()
  {
    // Arrange
    const int clubId = 8;
    var date = new DateOnly(2026, 2, 10);
    _clubRepository.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new ClubEntity { Id = clubId, Name = "Club B" });
    _matchRepository.GetRecentMatchesForClubAsync(clubId, 5, date, Arg.Any<CancellationToken>())
      .Returns(new List<Match>());

    // Act
    await _sut.Handle(new GetClubRollingPerformanceQuery(clubId, date), CancellationToken.None);

    // Assert
    await _matchRepository.Received(1).GetRecentMatchesForClubAsync(clubId, 5, date, Arg.Any<CancellationToken>());
  }

  private static string SerializePayload(double teamRating, string formation, params (string Name, double Rating)[] players)
  {
    var payload = new FotmobDetailsPayload(
      HomeLineup: new FotmobTeamLineup
      {
        TeamName = "Club A",
        TeamRating = teamRating,
        Formation = formation,
        Players = players.Select(p => new FotmobLineupPlayer { Name = p.Name, Rating = p.Rating }).ToList()
      },
      AwayLineup: null,
      Statistics: null,
      Players: null);
    return JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
  }
}
