using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Application.Common.Dto.Matches;
using NoMoreBets.Application.Matches;
using NoMoreBets.Application.Matches.UpdateUpcomming;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Leagues;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using DomainMatch = NoMoreBets.Domain.Matches.Match;
using DomainStage = NoMoreBets.Domain.Matches.Stage;
using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Application.Tests.Matches.UpdateUpcomming;

public class UpdateUpcommingMatchesHandlerTests
{
  private readonly IUpcommingMatchProvider _upcommingMatchProvider;
  private readonly IMatchMatcher _matchMatcher;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IMatchRepository _matchRepository;
  private readonly ILogger<UpdateUpcommingMatchesHandler> _logger;
  private readonly UpdateUpcommingMatchesHandler _sut;

  public UpdateUpcommingMatchesHandlerTests()
  {
    _upcommingMatchProvider = Substitute.For<IUpcommingMatchProvider>();
    _matchMatcher = Substitute.For<IMatchMatcher>();
    _unitOfWork = Substitute.For<IUnitOfWork>();
    _matchRepository = Substitute.For<IMatchRepository>();
    _logger = Substitute.For<ILogger<UpdateUpcommingMatchesHandler>>();
    _sut = new UpdateUpcommingMatchesHandler(_upcommingMatchProvider, _matchMatcher, _unitOfWork, _matchRepository, _logger);
  }

  [Fact]
  public async Task Handle_WhenTryParseMatchDateFails_SkipsMatch()
  {
    // Arrange: invalid date string so TryParseMatchDate returns false
    var previews = new List<LeagueMatchPreviews>
    {
      new()
      {
        LeagueId = 228,
        LeagueName = "Premier League",
        MatchPreviews =
        [
          new UpcomingMatchPreview
          {
            Id = 100,
            Date = "invalid-date",
            Time = "15:00",
            ExcitementRating = 5,
            Teams = new Teams { Home = new TeamInfo { Id = 1, Name = "Arsenal" }, Away = new TeamInfo { Id = 2, Name = "Chelsea" } }
          }
        ]
      }
    };
    _upcommingMatchProvider.GetMatchPreviewsUpcomingAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<LeagueMatchPreviews>>(previews));

    var leagues = new List<League> { new() { Id = 1, Name = "PL", SoccerdataId = 228 } };
    _unitOfWork.Leagues.GetLeagues().Returns(Task.FromResult(leagues));
    _unitOfWork.Clubs.GetBySoccerdataId(Arg.Any<IEnumerable<int>>()).Returns(Task.FromResult(new List<ClubEntity>()));
    _unitOfWork.Leagues.GetCurrentStage(228).Returns(Task.FromResult(new DomainStage { Id = 1 }));
    _matchRepository.GetMatches(Arg.Any<DateTime>()).Returns(_ => Task.FromResult(new List<DomainMatch>()));

    // Act
    var result = await _sut.Handle(new UpdateUpcommingMatchesCommand(228), CancellationToken.None);

    // Assert
    result.Should().BeEmpty();
    await _unitOfWork.Matches.DidNotReceive().AddMatch(Arg.Any<DomainMatch>());
  }

  [Fact]
  public async Task Handle_WhenLeagueNotInDb_SkipsLeagueMatches()
  {
    // Arrange: previews have LeagueId 999 but GetLeagues returns only league 228
    var previews = new List<LeagueMatchPreviews>
    {
      new()
      {
        LeagueId = 999,
        LeagueName = "Unknown League",
        MatchPreviews =
        [
          new UpcomingMatchPreview
          {
            Id = 100,
            Date = "15/01/2026",
            Time = "15:00",
            ExcitementRating = 5,
            Teams = new Teams { Home = new TeamInfo { Id = 1, Name = "A" }, Away = new TeamInfo { Id = 2, Name = "B" } }
          }
        ]
      }
    };
    _upcommingMatchProvider.GetMatchPreviewsUpcomingAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<LeagueMatchPreviews>>(previews));

    var leagues = new List<League> { new() { Id = 1, Name = "PL", SoccerdataId = 228 } };
    _unitOfWork.Leagues.GetLeagues().Returns(Task.FromResult(leagues));
    _unitOfWork.Clubs.GetBySoccerdataId(Arg.Any<IEnumerable<int>>()).ReturnsForAnyArgs(Task.FromResult(new List<ClubEntity>()));

    // Act
    var result = await _sut.Handle(new UpdateUpcommingMatchesCommand(null), CancellationToken.None);

    // Assert: league 999 not in GetLeagues so no matches added
    result.Should().BeEmpty();
  }

  [Fact]
  public async Task Handle_WhenClubMissingFromMap_SkipsMatchAndDoesNotAdd()
  {
    // Arrange: preview has Home.Id=1, Away.Id=2 but GetBySoccerdataId returns only club 1 (missing away)
    var previews = new List<LeagueMatchPreviews>
    {
      new()
      {
        LeagueId = 228,
        LeagueName = "Premier League",
        MatchPreviews =
        [
          new UpcomingMatchPreview
          {
            Id = 100,
            Date = "15/01/2026",
            Time = "15:00",
            ExcitementRating = 5,
            Teams = new Teams { Home = new TeamInfo { Id = 1, Name = "Arsenal" }, Away = new TeamInfo { Id = 2, Name = "Chelsea" } }
          }
        ]
      }
    };
    _upcommingMatchProvider.GetMatchPreviewsUpcomingAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<LeagueMatchPreviews>>(previews));

    var leagues = new List<League> { new() { Id = 1, Name = "PL", SoccerdataId = 228 } };
    _unitOfWork.Leagues.GetLeagues().Returns(Task.FromResult(leagues));
    var onlyHomeClub = new List<ClubEntity> { new() { Id = 10, Name = "Arsenal", LeagueId = 1, SoccerdataId = 1 } };
    _unitOfWork.Clubs.GetBySoccerdataId(Arg.Any<IEnumerable<int>>()).Returns(Task.FromResult(onlyHomeClub));
    _unitOfWork.Leagues.GetCurrentStage(228).Returns(Task.FromResult(new DomainStage { Id = 1 }));
    _matchRepository.GetMatches(Arg.Any<DateTime>()).Returns(_ => Task.FromResult(new List<DomainMatch>()));
    _matchMatcher.FindBestMatch("Arsenal", "Chelsea", Arg.Any<IReadOnlyList<(string HomeName, string AwayName, DomainMatch Value)>>()).Returns((DomainMatch?)null);

    // Act
    var result = await _sut.Handle(new UpdateUpcommingMatchesCommand(228), CancellationToken.None);

    // Assert
    result.Should().BeEmpty();
    await _unitOfWork.Matches.DidNotReceive().AddMatch(Arg.Any<DomainMatch>());
  }

  [Fact]
  public async Task Handle_WhenValidPreviewAndClubsExist_AddsMatchAndCallsSaveChanges()
  {
    // Arrange
    var previews = new List<LeagueMatchPreviews>
    {
      new()
      {
        LeagueId = 228,
        LeagueName = "Premier League",
        MatchPreviews =
        [
          new UpcomingMatchPreview
          {
            Id = 100,
            Date = "15/01/2026",
            Time = "15:00",
            ExcitementRating = 7.5,
            Teams = new Teams { Home = new TeamInfo { Id = 1, Name = "Arsenal" }, Away = new TeamInfo { Id = 2, Name = "Chelsea" } }
          }
        ]
      }
    };
    _upcommingMatchProvider.GetMatchPreviewsUpcomingAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<LeagueMatchPreviews>>(previews));

    var leagues = new List<League> { new() { Id = 1, Name = "PL", SoccerdataId = 228 } };
    _unitOfWork.Leagues.GetLeagues().Returns(Task.FromResult(leagues));
    var homeClub = new ClubEntity { Id = 10, Name = "Arsenal", LeagueId = 1, SoccerdataId = 1 };
    var awayClub = new ClubEntity { Id = 20, Name = "Chelsea", LeagueId = 1, SoccerdataId = 2 };
    _unitOfWork.Clubs.GetBySoccerdataId(Arg.Any<IEnumerable<int>>()).Returns(Task.FromResult(new List<ClubEntity> { homeClub, awayClub }));
    _unitOfWork.Leagues.GetCurrentStage(228).Returns(Task.FromResult(new DomainStage { Id = 5 }));
    _matchRepository.GetMatches(Arg.Any<DateTime>()).Returns(_ => Task.FromResult(new List<DomainMatch>()));
    _matchMatcher.FindBestMatch("Arsenal", "Chelsea", Arg.Any<IReadOnlyList<(string HomeName, string AwayName, DomainMatch Value)>>()).Returns((DomainMatch?)null);

    // Act
    var result = await _sut.Handle(new UpdateUpcommingMatchesCommand(228), CancellationToken.None);

    // Assert
    result.Should().HaveCount(1);
    result[0].SoccerdataId.Should().Be(100);
    result[0].HomeClubId.Should().Be(10);
    result[0].AwayClubId.Should().Be(20);
    result[0].StageId.Should().Be(5);
    await _unitOfWork.Matches.Received(1).AddMatch(Arg.Is<DomainMatch>(m => m.SoccerdataId == 100));
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenEmptyDateString_SkipsMatch()
  {
    // Arrange
    var previews = new List<LeagueMatchPreviews>
    {
      new()
      {
        LeagueId = 228,
        LeagueName = "Premier League",
        MatchPreviews =
        [
          new UpcomingMatchPreview
          {
            Id = 100,
            Date = "",
            Time = "15:00",
            ExcitementRating = 5,
            Teams = new Teams { Home = new TeamInfo { Id = 1, Name = "Arsenal" }, Away = new TeamInfo { Id = 2, Name = "Chelsea" } }
          }
        ]
      }
    };
    _upcommingMatchProvider.GetMatchPreviewsUpcomingAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<LeagueMatchPreviews>>(previews));
    var leagues = new List<League> { new() { Id = 1, Name = "PL", SoccerdataId = 228 } };
    _unitOfWork.Leagues.GetLeagues().Returns(Task.FromResult(leagues));
    _unitOfWork.Clubs.GetBySoccerdataId(Arg.Any<IEnumerable<int>>()).Returns(Task.FromResult(new List<ClubEntity>()));
    _unitOfWork.Leagues.GetCurrentStage(228).Returns(Task.FromResult(new DomainStage { Id = 1 }));

    // Act
    var result = await _sut.Handle(new UpdateUpcommingMatchesCommand(228), CancellationToken.None);

    // Assert
    result.Should().BeEmpty();
#pragma warning disable CS4014
    _matchRepository.DidNotReceive().GetMatches(Arg.Any<DateTime>());
#pragma warning restore CS4014
  }
}
