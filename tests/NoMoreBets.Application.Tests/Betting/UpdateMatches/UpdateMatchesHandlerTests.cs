using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Betting;
using NoMoreBets.Application.Betting.UpdateMatches;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Application.Common.Dto.Betting;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Matches;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Betting.UpdateMatches;

public class UpdateMatchesHandlerTests
{
  private readonly IBookmakerMatchesProvider _bookmakerMatchesProvider;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IMatchMatcher _matchMatcher;
  private readonly UpdateMatchesHandler _sut;

  public UpdateMatchesHandlerTests()
  {
    _bookmakerMatchesProvider = Substitute.For<IBookmakerMatchesProvider>();
    _unitOfWork = Substitute.For<IUnitOfWork>();
    _matchMatcher = Substitute.For<IMatchMatcher>();
    _sut = new UpdateMatchesHandler(_bookmakerMatchesProvider, _unitOfWork, _matchMatcher);
  }

  [Fact]
  public async Task Handle_WhenNoUpcomingGames_ReturnsEmptyList()
  {
    // Arrange
    _bookmakerMatchesProvider.GetUpcomingGamesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<UpcomingGame>>(new List<UpcomingGame>()));

    // Act
    var result = await _sut.Handle(new UpdateMatchesCommand(), CancellationToken.None);

    // Assert
    result.Should().BeEmpty();
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenExistingMatchFoundAndBetclicUrlEmpty_SetsBetclicUrlAndCallsSaveChangesOnce()
  {
    // Arrange
    var gameDate = new DateTime(2026, 1, 15, 14, 0, 0);
    var games = new List<UpcomingGame>
    {
      new() { Date = gameDate.Date, Time = "14:00", HomeTeam = "Arsenal", AwayTeam = "Chelsea", Url = "https://betclic.pl/match" }
    };
    _bookmakerMatchesProvider.GetUpcomingGamesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<UpcomingGame>>(games));

    var homeClub = new ClubEntity { Id = 1, Name = "Arsenal", LeagueId = 1, SoccerdataId = 1 };
    var awayClub = new ClubEntity { Id = 2, Name = "Chelsea", LeagueId = 1, SoccerdataId = 2 };
    _unitOfWork.Clubs.GetClubs(1).Returns(Task.FromResult(new List<ClubEntity> { homeClub, awayClub }));

    var existingMatch = Match.CreateUpcomming(DateTime.SpecifyKind(gameDate, DateTimeKind.Utc), 1, 1, 2);
    existingMatch.BetclicUrl = null;
    existingMatch.HomeClub = homeClub;
    existingMatch.AwayClub = awayClub;
    var matchesOnDay = new List<Match> { existingMatch };
    _unitOfWork.Matches.GetMatches(Arg.Any<DateTime>()).Returns(Task.FromResult(matchesOnDay));

    _matchMatcher.FindBestMatch("Arsenal", "Chelsea", Arg.Any<IReadOnlyList<(string HomeName, string AwayName, Match Value)>>())
      .Returns(existingMatch);

    // Act
    var result = await _sut.Handle(new UpdateMatchesCommand(), CancellationToken.None);

    // Assert
    result.Should().BeEmpty();
    existingMatch.BetclicUrl.Should().Be("https://betclic.pl/match");
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenFindClubThrows_PropagatesInvalidOperationException()
  {
    // Arrange
    var games = new List<UpcomingGame>
    {
      new() { Date = new DateTime(2026, 1, 15), Time = "15:00", HomeTeam = "Unknown Team", AwayTeam = "Chelsea", Url = "https://betclic.pl/match" }
    };
    _bookmakerMatchesProvider.GetUpcomingGamesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<UpcomingGame>>(games));

    var clubs = new List<ClubEntity> { new() { Id = 2, Name = "Chelsea", LeagueId = 1, SoccerdataId = 2 } };
    _unitOfWork.Clubs.GetClubs(1).Returns(Task.FromResult(clubs));
    _unitOfWork.Matches.GetMatches(Arg.Any<DateTime>()).Returns(Task.FromResult(new List<Match>()));

    _matchMatcher.FindBestMatch(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<(string HomeName, string AwayName, Match Value)>>())
      .Returns((Match?)null);
    _matchMatcher.FindClub("Unknown Team", clubs).Returns(_ => throw new InvalidOperationException("No matching club found"));

    // Act
    var act = () => _sut.Handle(new UpdateMatchesCommand(), CancellationToken.None);

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No matching club found*");
  }

  [Fact]
  public async Task Handle_WhenNewMatch_AddsMatchAndCallsSaveChanges()
  {
    // Arrange
    var gameDate = new DateTime(2026, 1, 15);
    var games = new List<UpcomingGame>
    {
      new() { Date = gameDate, Time = "15:00", HomeTeam = "Arsenal", AwayTeam = "Chelsea", Url = "https://betclic.pl/arsenal-chelsea" }
    };
    _bookmakerMatchesProvider.GetUpcomingGamesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<UpcomingGame>>(games));

    var homeClub = new ClubEntity { Id = 1, Name = "Arsenal", LeagueId = 1, SoccerdataId = 1 };
    var awayClub = new ClubEntity { Id = 2, Name = "Chelsea", LeagueId = 1, SoccerdataId = 2 };
    var clubs = new List<ClubEntity> { homeClub, awayClub };
    _unitOfWork.Clubs.GetClubs(1).Returns(Task.FromResult(clubs));
    _unitOfWork.Matches.GetMatches(Arg.Any<DateTime>()).Returns(Task.FromResult(new List<Match>()));

    _matchMatcher.FindBestMatch("Arsenal", "Chelsea", Arg.Any<IReadOnlyList<(string HomeName, string AwayName, Match Value)>>())
      .Returns((Match?)null);
    _matchMatcher.FindClub("Arsenal", clubs).Returns(homeClub);
    _matchMatcher.FindClub("Chelsea", clubs).Returns(awayClub);

    // Act
    var result = await _sut.Handle(new UpdateMatchesCommand(), CancellationToken.None);

    // Assert
    result.Should().HaveCount(1);
    result[0].HomeClubId.Should().Be(1);
    result[0].AwayClubId.Should().Be(2);
    result[0].BetclicUrl.Should().Be("https://betclic.pl/arsenal-chelsea");
    await _unitOfWork.Matches.Received(1).AddMatch(Arg.Is<Match>(m => m.HomeClubId == 1 && m.AwayClubId == 2));
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenGameHasEmptyTime_UsesDateOnlyForMatchDate()
  {
    // Arrange: time is empty, so CombineDateAndTime returns date without time
    var gameDate = new DateTime(2026, 1, 15);
    var games = new List<UpcomingGame>
    {
      new() { Date = gameDate, Time = "", HomeTeam = "Arsenal", AwayTeam = "Chelsea", Url = "https://betclic.pl/m" }
    };
    _bookmakerMatchesProvider.GetUpcomingGamesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<UpcomingGame>>(games));

    var homeClub = new ClubEntity { Id = 1, Name = "Arsenal", LeagueId = 1, SoccerdataId = 1 };
    var awayClub = new ClubEntity { Id = 2, Name = "Chelsea", LeagueId = 1, SoccerdataId = 2 };
    _unitOfWork.Clubs.GetClubs(1).Returns(Task.FromResult(new List<ClubEntity> { homeClub, awayClub }));
    _unitOfWork.Matches.GetMatches(Arg.Any<DateTime>()).Returns(Task.FromResult(new List<Match>()));

    _matchMatcher.FindBestMatch(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<(string HomeName, string AwayName, Match Value)>>()).Returns((Match?)null);
    _matchMatcher.FindClub("Arsenal", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(homeClub);
    _matchMatcher.FindClub("Chelsea", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(awayClub);

    // Act
    var result = await _sut.Handle(new UpdateMatchesCommand(), CancellationToken.None);

    // Assert: match date should be gameDate at midnight (empty time => date only)
    result.Should().HaveCount(1);
    result[0].MatchDate.Should().Be(DateTime.SpecifyKind(gameDate.Date, DateTimeKind.Utc));
  }

  [Fact]
  public async Task Handle_WhenGameHasValidTime_CombinesDateAndTime()
  {
    // Arrange
    var gameDate = new DateTime(2026, 1, 15);
    var games = new List<UpcomingGame>
    {
      new() { Date = gameDate, Time = "14:30", HomeTeam = "Arsenal", AwayTeam = "Chelsea", Url = "https://betclic.pl/m" }
    };
    _bookmakerMatchesProvider.GetUpcomingGamesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<UpcomingGame>>(games));

    var homeClub = new ClubEntity { Id = 1, Name = "Arsenal", LeagueId = 1, SoccerdataId = 1 };
    var awayClub = new ClubEntity { Id = 2, Name = "Chelsea", LeagueId = 1, SoccerdataId = 2 };
    _unitOfWork.Clubs.GetClubs(1).Returns(Task.FromResult(new List<ClubEntity> { homeClub, awayClub }));
    _unitOfWork.Matches.GetMatches(Arg.Any<DateTime>()).Returns(Task.FromResult(new List<Match>()));

    _matchMatcher.FindBestMatch(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<(string HomeName, string AwayName, Match Value)>>()).Returns((Match?)null);
    _matchMatcher.FindClub("Arsenal", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(homeClub);
    _matchMatcher.FindClub("Chelsea", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(awayClub);

    // Act
    var result = await _sut.Handle(new UpdateMatchesCommand(), CancellationToken.None);

    // Assert: 14:30 combined with date
    var expectedDate = DateTime.SpecifyKind(new DateTime(2026, 1, 15, 14, 30, 0), DateTimeKind.Utc);
    result.Should().HaveCount(1);
    result[0].MatchDate.Should().Be(expectedDate);
  }
}
