using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto.Leagues;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Application.Matches;
using NoMoreBets.Application.Matches.UpdateMatchDetails;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Matches.UpdateMatchDetails;

public class UpdateMatchDetailsHandlerTests
{
  private readonly IMatchDetailsProvider _matchDetailsProvider;
  private readonly IMatchMatcher _matchMatcher;
  private readonly IUnitOfWork _unitOfWork;
  private readonly ILogger<UpdateMatchDetailsHandler> _logger;
  private readonly UpdateMatchDetailsHandler _sut;

  public UpdateMatchDetailsHandlerTests()
  {
    _matchDetailsProvider = Substitute.For<IMatchDetailsProvider>();
    _matchMatcher = Substitute.For<IMatchMatcher>();
    _unitOfWork = Substitute.For<IUnitOfWork>();
    _logger = Substitute.For<ILogger<UpdateMatchDetailsHandler>>();
    _sut = new UpdateMatchDetailsHandler(_matchDetailsProvider, _matchMatcher, _unitOfWork, _logger);

    _unitOfWork.Leagues.GetLeagues().Returns(new List<League>
    {
      new() { Id = 1, Name = "Premier League", Slug = "premier-league", SoccerdataId = 228 }
    });
    _unitOfWork.Leagues.GetCurrentStage(228).Returns(new Stage { Id = 1, SeasonId = 1, Name = "Premier League", SoccerdataId = 13908 });
  }

  [Fact]
  public async Task Handle_WhenFotmobGameUrlNullOrWhiteSpace_ReturnsWithoutCallingProvider()
  {
    // Act
    await _sut.Handle(new UpdateMatchDetailsCommand(""), CancellationToken.None);
    await _sut.Handle(new UpdateMatchDetailsCommand("   "), CancellationToken.None);
    await _sut.Handle(new UpdateMatchDetailsCommand(null!), CancellationToken.None);

    // Assert
    await _matchDetailsProvider.DidNotReceive().GetMatchDetailsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenProviderThrows_PropagatesException()
  {
    _matchDetailsProvider.GetMatchDetailsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns(_ => Task.FromException<MatchDetailsDto>(new InvalidOperationException("Fotmob down")));

    var act = () => _sut.Handle(new UpdateMatchDetailsCommand("https://fotmob.com/match/1"), CancellationToken.None);

    await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Fotmob down*");
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

[Fact]
public async Task Handle_WhenExistingDetailsByFotmobUrl_ReturnsWithoutUpdatingOrSaving()
  {
  // Arrange
    var url = "https://fotmob.com/match/1";
    var dto = new MatchDetailsDto
    {
      HomeTeam = "Arsenal",
      AwayTeam = "Chelsea",
      MatchDate = DateTimeOffset.UtcNow
    };
    _matchDetailsProvider.GetMatchDetailsAsync(url, Arg.Any<CancellationToken>()).Returns(dto);

    var existingMatch = new Match { Id = 42 };
    var existingDetails = new MatchDetails { Id = 1, MatchId = 42, Match = existingMatch, FotmobUrl = url, FotmobDetailsJson = "old" };
    _unitOfWork.Matches.GetMatchDetailsByFotmobUrlAsync(url, Arg.Any<CancellationToken>()).Returns(existingDetails);

  // Act
    await _sut.Handle(new UpdateMatchDetailsCommand(url), CancellationToken.None);

  // Assert
  existingDetails.FotmobDetailsJson.Should().Be("old");
    existingDetails.FotmobUrl.Should().Be(url);
  await _matchDetailsProvider.DidNotReceive().GetMatchDetailsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
  await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    await _unitOfWork.Matches.DidNotReceive().AddMatch(Arg.Any<Match>(), Arg.Any<CancellationToken>());
    await _unitOfWork.Matches.DidNotReceive().AddMatchDetailsAsync(Arg.Any<MatchDetails>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenNoMatchDateInDto_ReturnsWithoutInsert()
  {
    var url = "https://fotmob.com/match/1";
    var dto = new MatchDetailsDto { HomeTeam = "Arsenal", AwayTeam = "Chelsea", MatchDate = null };
    _matchDetailsProvider.GetMatchDetailsAsync(url, Arg.Any<CancellationToken>()).Returns(dto);
    _unitOfWork.Matches.GetMatchDetailsByFotmobUrlAsync(url, Arg.Any<CancellationToken>()).Returns((MatchDetails?)null);

    var result = await _sut.Handle(new UpdateMatchDetailsCommand(url), CancellationToken.None);

    result.Should().Be(Unit.Value);
    await _unitOfWork.Matches.DidNotReceive().AddMatch(Arg.Any<Match>(), Arg.Any<CancellationToken>());
    await _unitOfWork.Matches.DidNotReceive().AddMatchDetailsAsync(Arg.Any<MatchDetails>(), Arg.Any<CancellationToken>());
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenFindClubThrows_ReturnsWithoutInsert()
  {
    var url = "https://fotmob.com/match/1";
    var dto = new MatchDetailsDto
    {
      HomeTeam = "Arsenal",
      AwayTeam = "Chelsea",
      MatchDate = DateTimeOffset.UtcNow
    };
    _matchDetailsProvider.GetMatchDetailsAsync(url, Arg.Any<CancellationToken>()).Returns(dto);
    _unitOfWork.Matches.GetMatchDetailsByFotmobUrlAsync(url, Arg.Any<CancellationToken>()).Returns((MatchDetails?)null);
    _unitOfWork.Matches.GetMatches(Arg.Any<DateTime>()).Returns(new List<Match>());
    var clubs = new List<ClubEntity> { new() { Id = 2, Name = "Chelsea", LeagueId = 1, SoccerdataId = 2 } };
    _unitOfWork.Clubs.GetClubs().Returns(Task.FromResult(clubs));
    _matchMatcher.FindClub("Arsenal", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(_ => throw new InvalidOperationException("No club"));

    var result = await _sut.Handle(new UpdateMatchDetailsCommand(url), CancellationToken.None);

    result.Should().Be(Unit.Value);
    await _unitOfWork.Matches.DidNotReceive().AddMatch(Arg.Any<Match>(), Arg.Any<CancellationToken>());
    await _unitOfWork.Matches.DidNotReceive().AddMatchDetailsAsync(Arg.Any<MatchDetails>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenMatchFoundByTeamsAndDate_ExistingDetails_UpdatesDetailsAndSaveChanges()
  {
    var url = "https://fotmob.com/match/1";
    var dto = new MatchDetailsDto
    {
      HomeTeam = "Arsenal",
      AwayTeam = "Chelsea",
      MatchDate = DateTimeOffset.UtcNow
    };
    _matchDetailsProvider.GetMatchDetailsAsync(url, Arg.Any<CancellationToken>()).Returns(dto);
    _unitOfWork.Matches.GetMatchDetailsByFotmobUrlAsync(url, Arg.Any<CancellationToken>()).Returns((MatchDetails?)null);

    var homeClub = new ClubEntity { Id = 1, Name = "Arsenal", LeagueId = 1, SoccerdataId = 1 };
    var awayClub = new ClubEntity { Id = 2, Name = "Chelsea", LeagueId = 1, SoccerdataId = 2 };
    var existingMatch = new Match { Id = 10, HomeClub = homeClub, AwayClub = awayClub };
    var matchesOnDay = new List<Match> { existingMatch };
    _unitOfWork.Matches.GetMatches(Arg.Any<DateTime>()).Returns(matchesOnDay);
    _matchMatcher.FindBestMatch("Arsenal", "Chelsea", Arg.Any<IReadOnlyList<(string HomeName, string AwayName, Match Value)>>()).Returns(existingMatch);

    var existingDetails = new MatchDetails { Id = 2, MatchId = 10, FotmobDetailsJson = "old" };
    _unitOfWork.Matches.GetMatchDetailsByMatchIdAsync(10, Arg.Any<CancellationToken>()).Returns(existingDetails);

    await _sut.Handle(new UpdateMatchDetailsCommand(url), CancellationToken.None);

    existingDetails.FotmobUrl.Should().Be(url);
    existingDetails.FotmobDetailsJson.Should().NotBe("old");
    await _unitOfWork.Matches.DidNotReceive().AddMatchDetailsAsync(Arg.Any<MatchDetails>(), Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenMatchFoundByTeamsAndDate_NoDetails_AddsDetailsAndSaveChanges()
  {
    var url = "https://fotmob.com/match/1";
    var dto = new MatchDetailsDto
    {
      HomeTeam = "Arsenal",
      AwayTeam = "Chelsea",
      MatchDate = DateTimeOffset.UtcNow
    };
    _matchDetailsProvider.GetMatchDetailsAsync(url, Arg.Any<CancellationToken>()).Returns(dto);
    _unitOfWork.Matches.GetMatchDetailsByFotmobUrlAsync(url, Arg.Any<CancellationToken>()).Returns((MatchDetails?)null);

    var homeClub = new ClubEntity { Id = 1, Name = "Arsenal", LeagueId = 1, SoccerdataId = 1 };
    var awayClub = new ClubEntity { Id = 2, Name = "Chelsea", LeagueId = 1, SoccerdataId = 2 };
    var existingMatch = new Match { Id = 10, HomeClub = homeClub, AwayClub = awayClub };
    _unitOfWork.Matches.GetMatches(Arg.Any<DateTime>()).Returns(new List<Match> { existingMatch });
    _matchMatcher.FindBestMatch("Arsenal", "Chelsea", Arg.Any<IReadOnlyList<(string HomeName, string AwayName, Match Value)>>()).Returns(existingMatch);
    _unitOfWork.Matches.GetMatchDetailsByMatchIdAsync(10, Arg.Any<CancellationToken>()).Returns((MatchDetails?)null);

    await _sut.Handle(new UpdateMatchDetailsCommand(url), CancellationToken.None);

    await _unitOfWork.Matches.Received(1).AddMatchDetailsAsync(
      Arg.Is<MatchDetails>(d => d.MatchId == 10 && d.FotmobUrl == url && !string.IsNullOrEmpty(d.FotmobDetailsJson)),
      Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    await _unitOfWork.Matches.DidNotReceive().AddMatch(Arg.Any<Match>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenNoExistingMatch_InsertsMatchAndDetailsAndCallsSaveChanges()
  {
    var url = "https://fotmob.com/match/1";
    var dto = new MatchDetailsDto
    {
      HomeTeam = "Arsenal",
      AwayTeam = "Chelsea",
      MatchDate = DateTimeOffset.UtcNow
    };
    _matchDetailsProvider.GetMatchDetailsAsync(url, Arg.Any<CancellationToken>()).Returns(dto);
    _unitOfWork.Matches.GetMatchDetailsByFotmobUrlAsync(url, Arg.Any<CancellationToken>()).Returns((MatchDetails?)null);
    _unitOfWork.Matches.GetMatches(Arg.Any<DateTime>()).Returns(new List<Match>());

    var homeClub = new ClubEntity { Id = 1, Name = "Arsenal", LeagueId = 1, SoccerdataId = 1 };
    var awayClub = new ClubEntity { Id = 2, Name = "Chelsea", LeagueId = 1, SoccerdataId = 2 };
    _unitOfWork.Clubs.GetClubs().Returns(Task.FromResult(new List<ClubEntity> { homeClub, awayClub }));
    _matchMatcher.FindClub("Arsenal", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(homeClub);
    _matchMatcher.FindClub("Chelsea", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(awayClub);

    await _sut.Handle(new UpdateMatchDetailsCommand(url), CancellationToken.None);

    await _unitOfWork.Matches.Received(1).AddMatch(Arg.Is<Match>(m => m.HomeClubId == 1 && m.AwayClubId == 2), Arg.Any<CancellationToken>());
    await _unitOfWork.Matches.Received(1).AddMatchDetailsAsync(Arg.Is<MatchDetails>(d => d.FotmobUrl == url), Arg.Any<CancellationToken>());
    await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
