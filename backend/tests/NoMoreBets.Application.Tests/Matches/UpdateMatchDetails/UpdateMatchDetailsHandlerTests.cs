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

    _unitOfWork.Leagues.GetStageForDateAsync(League.UnknownSoccerdataId, Arg.Any<DateOnly>())
      .Returns(new Stage { Id = 8, SeasonId = 8, Name = "Unknown", SoccerdataId = 0 });
    _unitOfWork.Leagues.GetLeagues()
      .Returns(new List<League> { new() { Id = 8, Name = "Unknown", Slug = League.UnknownSlug, SoccerdataId = League.UnknownSoccerdataId } });
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

    result.CreatedNewMatch.Should().BeFalse();
    await _unitOfWork.Matches.DidNotReceive().AddMatch(Arg.Any<Match>(), Arg.Any<CancellationToken>());
    await _unitOfWork.Matches.DidNotReceive().AddMatchDetailsAsync(Arg.Any<MatchDetails>(), Arg.Any<CancellationToken>());
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenFindClubThrows_CreatesUnknownClubAndInsertsMatch()
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
    var awayClub = new ClubEntity { Id = 2, Name = "Chelsea", Slug = "chelsea", SoccerdataId = 2 };
    _unitOfWork.Clubs.GetClubs().Returns(Task.FromResult(new List<ClubEntity> { awayClub }));
    _matchMatcher.FindClub("Arsenal", Arg.Any<IReadOnlyList<ClubEntity>>())
      .Returns(_ => throw new ClubMatchNotFoundException("Arsenal", "No matching club found for 'Arsenal'"));
    _matchMatcher.FindClub("Chelsea", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(awayClub);
    StubAddClubAssignsIds(startingId: 100);

    var result = await _sut.Handle(new UpdateMatchDetailsCommand(url), CancellationToken.None);

    result.CreatedNewMatch.Should().BeTrue();
    await _unitOfWork.Clubs.Received(1).AddClubAsync(
      Arg.Is<ClubEntity>(c => c.Name == "Arsenal"
        && c.Slug == "arsenal"
        && c.ClubSeasons.Any(cs => cs.SeasonId == 8)),
      Arg.Any<CancellationToken>());
    await _unitOfWork.Matches.Received(1).AddMatch(
      Arg.Is<Match>(m => m.HomeClubId == 100 && m.AwayClubId == 2 && m.StageId == 8),
      Arg.Any<CancellationToken>());
    await _unitOfWork.Matches.Received(1).AddMatchDetailsAsync(Arg.Is<MatchDetails>(d => d.FotmobUrl == url), Arg.Any<CancellationToken>());
    await _unitOfWork.Received(3).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenBothClubsUnresolved_CreatesTwoUnknownClubsAndInsertsMatch()
  {
    var url = "https://fotmob.com/match/1";
    var dto = new MatchDetailsDto
    {
      HomeTeam = "Team Alpha",
      AwayTeam = "Team Beta",
      MatchDate = DateTimeOffset.UtcNow
    };
    _matchDetailsProvider.GetMatchDetailsAsync(url, Arg.Any<CancellationToken>()).Returns(dto);
    _unitOfWork.Matches.GetMatchDetailsByFotmobUrlAsync(url, Arg.Any<CancellationToken>()).Returns((MatchDetails?)null);
    _unitOfWork.Matches.GetMatches(Arg.Any<DateTime>()).Returns(new List<Match>());
    _unitOfWork.Clubs.GetClubs().Returns(Task.FromResult(new List<ClubEntity>()));
    _matchMatcher.FindClub("Team Beta", Arg.Any<IReadOnlyList<ClubEntity>>())
      .Returns(_ => throw new ClubMatchNotFoundException("Team Beta", "No matching club found for 'Team Beta'"));
    StubAddClubAssignsIds(startingId: 100);

    var result = await _sut.Handle(new UpdateMatchDetailsCommand(url), CancellationToken.None);

    result.CreatedNewMatch.Should().BeTrue();
    await _unitOfWork.Clubs.Received(2).AddClubAsync(
      Arg.Is<ClubEntity>(c => c.ClubSeasons.Any(cs => cs.SeasonId == 8)),
      Arg.Any<CancellationToken>());
    await _unitOfWork.Matches.Received(1).AddMatch(
      Arg.Is<Match>(m => m.HomeClubId == 100 && m.AwayClubId == 101 && m.StageId == 8),
      Arg.Any<CancellationToken>());
    await _unitOfWork.Received(3).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenHomeClubResolvedAndAwayUnresolved_CreatesOneUnknownClubAndInsertsMatch()
  {
    var url = "https://fotmob.com/match/1";
    var dto = new MatchDetailsDto
    {
      HomeTeam = "Arsenal",
      AwayTeam = "Unknown FC",
      MatchDate = DateTimeOffset.UtcNow
    };
    _matchDetailsProvider.GetMatchDetailsAsync(url, Arg.Any<CancellationToken>()).Returns(dto);
    _unitOfWork.Matches.GetMatchDetailsByFotmobUrlAsync(url, Arg.Any<CancellationToken>()).Returns((MatchDetails?)null);
    _unitOfWork.Matches.GetMatches(Arg.Any<DateTime>()).Returns(new List<Match>());
    var homeClub = new ClubEntity { Id = 1, Name = "Arsenal", Slug = "arsenal", SoccerdataId = 1 };
    _unitOfWork.Clubs.GetClubs().Returns(Task.FromResult(new List<ClubEntity> { homeClub }));
    _matchMatcher.FindClub("Arsenal", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(homeClub);
    _matchMatcher.FindClub("Unknown FC", Arg.Any<IReadOnlyList<ClubEntity>>())
      .Returns(_ => throw new ClubMatchNotFoundException("Unknown FC", "No matching club found for 'Unknown FC'"));
    StubAddClubAssignsIds(startingId: 200);

    var result = await _sut.Handle(new UpdateMatchDetailsCommand(url), CancellationToken.None);

    result.CreatedNewMatch.Should().BeTrue();
    await _unitOfWork.Clubs.Received(1).AddClubAsync(
      Arg.Is<ClubEntity>(c => c.Name == "Unknown FC" && c.ClubSeasons.Any(cs => cs.SeasonId == 8)),
      Arg.Any<CancellationToken>());
    await _unitOfWork.Matches.Received(1).AddMatch(
      Arg.Is<Match>(m => m.HomeClubId == 1 && m.AwayClubId == 200 && m.StageId == 8),
      Arg.Any<CancellationToken>());
    await _unitOfWork.Received(3).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  private void StubAddClubAssignsIds(int startingId)
  {
    var nextClubId = startingId;
    _unitOfWork.Clubs.AddClubAsync(Arg.Any<ClubEntity>(), Arg.Any<CancellationToken>())
      .Returns(callInfo =>
      {
        callInfo.ArgAt<ClubEntity>(0).Id = nextClubId++;
        return Task.CompletedTask;
      });
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

    var homeClub = new ClubEntity { Id = 1, Name = "Arsenal", SoccerdataId = 1 };
    var awayClub = new ClubEntity { Id = 2, Name = "Chelsea", SoccerdataId = 2 };
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

    var homeClub = new ClubEntity { Id = 1, Name = "Arsenal", SoccerdataId = 1 };
    var awayClub = new ClubEntity { Id = 2, Name = "Chelsea", SoccerdataId = 2 };
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

    var homeClub = new ClubEntity { Id = 1, Name = "Arsenal", SoccerdataId = 1 };
    var awayClub = new ClubEntity { Id = 2, Name = "Chelsea", SoccerdataId = 2 };
    _unitOfWork.Clubs.GetClubs().Returns(Task.FromResult(new List<ClubEntity> { homeClub, awayClub }));
    _matchMatcher.FindClub("Arsenal", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(homeClub);
    _matchMatcher.FindClub("Chelsea", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(awayClub);

    var result = await _sut.Handle(new UpdateMatchDetailsCommand(url), CancellationToken.None);

    result.CreatedNewMatch.Should().BeTrue();
    await _unitOfWork.Leagues.Received(1)
      .GetStageForDateAsync(League.UnknownSoccerdataId, Arg.Any<DateOnly>());
    await _unitOfWork.Matches.Received(1).AddMatch(
      Arg.Is<Match>(m => m.HomeClubId == 1 && m.AwayClubId == 2 && m.StageId == 8),
      Arg.Any<CancellationToken>());
    await _unitOfWork.Matches.Received(1).AddMatchDetailsAsync(Arg.Is<MatchDetails>(d => d.FotmobUrl == url), Arg.Any<CancellationToken>());
    await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenNoExistingMatch_AndClubsFromDifferentLeagues_InsertsMatchUnderUnknownLeague()
  {
    var url = "https://fotmob.com/match/1";
    var dto = new MatchDetailsDto
    {
      HomeTeam = "Arsenal",
      AwayTeam = "Real Madrid",
      MatchDate = DateTimeOffset.UtcNow
    };
    _matchDetailsProvider.GetMatchDetailsAsync(url, Arg.Any<CancellationToken>()).Returns(dto);
    _unitOfWork.Matches.GetMatchDetailsByFotmobUrlAsync(url, Arg.Any<CancellationToken>()).Returns((MatchDetails?)null);
    _unitOfWork.Matches.GetMatches(Arg.Any<DateTime>()).Returns(new List<Match>());

    var homeClub = new ClubEntity { Id = 1, Name = "Arsenal", SoccerdataId = 1 };
    var awayClub = new ClubEntity { Id = 2, Name = "Real Madrid", SoccerdataId = 2 };
    _unitOfWork.Clubs.GetClubs().Returns(Task.FromResult(new List<ClubEntity> { homeClub, awayClub }));
    _matchMatcher.FindClub("Arsenal", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(homeClub);
    _matchMatcher.FindClub("Real Madrid", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(awayClub);

    var result = await _sut.Handle(new UpdateMatchDetailsCommand(url), CancellationToken.None);

    result.CreatedNewMatch.Should().BeTrue();
    await _unitOfWork.Matches.Received(1).AddMatch(
      Arg.Is<Match>(m => m.StageId == 8),
      Arg.Any<CancellationToken>());
  }
}
