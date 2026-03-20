using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Clubs.GetClubRecentGames;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Matches;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Club.GetClubRecentGames;

public class GetClubRecentGamesHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IClubRepository _clubRepository = Substitute.For<IClubRepository>();
  private readonly IMatchRepository _matchRepository = Substitute.For<IMatchRepository>();
  private readonly GetClubRecentGamesHandler _sut;

  public GetClubRecentGamesHandlerTests()
  {
    _unitOfWork.Clubs.Returns(_clubRepository);
    _unitOfWork.Matches.Returns(_matchRepository);
    _sut = new GetClubRecentGamesHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WithDate_DispatchesRepositoryWithDateAndReturnsMappedResults()
  {
    // Arrange
    const int clubId = 7;
    var date = new DateOnly(2026, 3, 10);
    _clubRepository.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new ClubEntity { Id = clubId, Name = "Club A" });

    var olderMatch = CreateFinishedMatch(matchId: 101, clubId: clubId, date: new DateTime(2026, 3, 5), homeGoals: 1, awayGoals: 0, isHome: true);
    var newerMatch = CreateFinishedMatch(matchId: 102, clubId: clubId, date: new DateTime(2026, 3, 9), homeGoals: 2, awayGoals: 2, isHome: false);
    _matchRepository.GetRecentMatchesForClubAsync(clubId, 5, date, Arg.Any<CancellationToken>())
      .Returns(new List<Match> { olderMatch, newerMatch });

    // Act
    var result = await _sut.Handle(new GetClubRecentGamesQuery(clubId, date), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    var recentGames = result!;
    recentGames.Should().HaveCount(2);
    recentGames.Select(m => m.MatchId).Should().Equal(102, 101);
    await _matchRepository.Received(1).GetRecentMatchesForClubAsync(clubId, 5, date, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenNoGamesBeforeDate_ReturnsEmptyArray()
  {
    // Arrange
    const int clubId = 9;
    var date = new DateOnly(2026, 2, 1);
    _clubRepository.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new ClubEntity { Id = clubId, Name = "Club A" });
    _matchRepository.GetRecentMatchesForClubAsync(clubId, 5, date, Arg.Any<CancellationToken>())
      .Returns(new List<Match>());

    // Act
    var result = await _sut.Handle(new GetClubRecentGamesQuery(clubId, date), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
  }

  private static Match CreateFinishedMatch(int matchId, int clubId, DateTime date, int homeGoals, int awayGoals, bool isHome)
  {
    return new Match
    {
      Id = matchId,
      MatchDate = DateTime.SpecifyKind(date, DateTimeKind.Utc),
      HomeClubId = isHome ? clubId : 1000 + matchId,
      AwayClubId = isHome ? 1000 + matchId : clubId,
      HomeGoals = homeGoals,
      AwayGoals = awayGoals,
      HomeClub = new ClubEntity { Id = isHome ? clubId : 1000 + matchId, Name = isHome ? "Club A" : $"Opponent {matchId}" },
      AwayClub = new ClubEntity { Id = isHome ? 1000 + matchId : clubId, Name = isHome ? $"Opponent {matchId}" : "Club A" }
    };
  }
}
