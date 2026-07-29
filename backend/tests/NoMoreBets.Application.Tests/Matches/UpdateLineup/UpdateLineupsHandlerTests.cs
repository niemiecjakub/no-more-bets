using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Application.Matches.UpdateLineup;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Matches.UpdateLineup;

public class UpdateLineupsHandlerTests
{
  private const int SupportedLeagueId = 42;
  private const string SupportedLeagueSlug = "premier-league";
  private readonly ILineupProvider _lineupProvider;
  private readonly IMatchMatcher _matchMatcher;
  private readonly IUnitOfWork _unitOfWork;
  private readonly ILeagueRepository _leagueRepository;
  private readonly ILogger<UpdateLineupsHandler> _logger;
  private readonly UpdateLineupsHandler _sut;

  public UpdateLineupsHandlerTests()
  {
    _lineupProvider = Substitute.For<ILineupProvider>();
    _matchMatcher = Substitute.For<IMatchMatcher>();
    _unitOfWork = Substitute.For<IUnitOfWork>();
    _leagueRepository = Substitute.For<ILeagueRepository>();
    _logger = Substitute.For<ILogger<UpdateLineupsHandler>>();
    _unitOfWork.Leagues.Returns(_leagueRepository);
    _leagueRepository.GetByIdAsync(SupportedLeagueId, Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<League?>(new League
      {
        Id = SupportedLeagueId,
        Slug = SupportedLeagueSlug,
        Name = "Premier League"
      }));
    _lineupProvider.SupportedLeagueSlugs.Returns(new[] { "premier-league" });
    _sut = new UpdateLineupsHandler(_lineupProvider, _matchMatcher, _unitOfWork, _logger);
  }

  private void SetupInWindowSeason()
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    _leagueRepository.GetLatestSeasonAsync(SupportedLeagueId, Arg.Any<CancellationToken>())
      .Returns(new Season
      {
        Id = 1,
        LeagueId = SupportedLeagueId,
        Year = "2025",
        StartDate = today.AddDays(-30),
        EndDate = today.AddDays(100)
      });
  }

  private static GameLineup CreateLineup(DateTime date, string home = "Arsenal", string away = "Chelsea")
  {
    var homeTeam = new TeamLineup { LineupType = LineupType.Predicted, Players = Array.Empty<PlayerInLineup>() };
    var awayTeam = new TeamLineup { LineupType = LineupType.Predicted, Players = Array.Empty<PlayerInLineup>() };
    return new GameLineup
    {
      Date = date,
      HomeTeamName = home,
      AwayTeamName = away,
      HomeTeam = homeTeam,
      AwayTeam = awayTeam
    };
  }

  [Fact]
  public async Task Handle_WhenNoSeasonExists_SkipsScrape()
  {
    _leagueRepository.GetLatestSeasonAsync(SupportedLeagueId, Arg.Any<CancellationToken>())
      .Returns((Season?)null);

    await _sut.Handle(new UpdateLineupsCommand(SupportedLeagueId), CancellationToken.None);

    await _lineupProvider.DidNotReceive()
      .GetSoccerLineupsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenLatestSeasonOutsideFetchWindow_SkipsScrape()
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    _leagueRepository.GetLatestSeasonAsync(SupportedLeagueId, Arg.Any<CancellationToken>())
      .Returns(new Season
      {
        Id = 1,
        LeagueId = SupportedLeagueId,
        Year = "2025",
        StartDate = today.AddDays(14),
        EndDate = today.AddDays(200)
      });

    await _sut.Handle(new UpdateLineupsCommand(SupportedLeagueId), CancellationToken.None);

    await _lineupProvider.DidNotReceive()
      .GetSoccerLineupsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenSeasonEndedYesterday_SkipsScrape()
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    _leagueRepository.GetLatestSeasonAsync(SupportedLeagueId, Arg.Any<CancellationToken>())
      .Returns(new Season
      {
        Id = 1,
        LeagueId = SupportedLeagueId,
        Year = "2025",
        StartDate = today.AddDays(-200),
        EndDate = today.AddDays(-1)
      });

    await _sut.Handle(new UpdateLineupsCommand(SupportedLeagueId), CancellationToken.None);

    await _lineupProvider.DidNotReceive()
      .GetSoccerLineupsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenNoLineups_CompletesWithoutAddOrUpdate()
  {
    SetupInWindowSeason();
    _lineupProvider.GetSoccerLineupsAsync(SupportedLeagueSlug, Arg.Any<CancellationToken>())
      .Returns(Array.Empty<GameLineup>());

    await _sut.Handle(new UpdateLineupsCommand(SupportedLeagueId), CancellationToken.None);

    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    await _unitOfWork.Matches.DidNotReceive().AddLineup(Arg.Any<Lineup>());
  }

  [Fact]
  public async Task Handle_WhenAllLineupsUnmatched_CompletesWithoutInsert()
  {
    SetupInWindowSeason();
    var date = new DateTime(2026, 1, 15, 14, 0, 0, DateTimeKind.Utc);
    _lineupProvider.GetSoccerLineupsAsync(SupportedLeagueSlug, Arg.Any<CancellationToken>())
      .Returns(new[] { CreateLineup(date) });
    _unitOfWork.Matches.GetMatches(Arg.Any<DateTime>()).Returns(new List<Match>());
    _matchMatcher.FindBestMatch(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<(string HomeName, string AwayName, Match Value)>>()).Returns((Match?)null);

    await _sut.Handle(new UpdateLineupsCommand(SupportedLeagueId), CancellationToken.None);

    await _unitOfWork.Matches.DidNotReceive().AddLineup(Arg.Any<Lineup>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenOneMatchFound_NoExistingLineup_AddsLineupAndSaveChanges()
  {
    SetupInWindowSeason();
    var date = new DateTime(2026, 1, 15, 14, 0, 0, DateTimeKind.Utc);
    var lineup = CreateLineup(date);
    _lineupProvider.GetSoccerLineupsAsync(SupportedLeagueSlug, Arg.Any<CancellationToken>())
      .Returns(new[] { lineup });

    var match = new Match { Id = 10, HomeClub = new ClubEntity { Name = "Arsenal" }, AwayClub = new ClubEntity { Name = "Chelsea" } };
    _unitOfWork.Matches.GetMatches(date).Returns(new List<Match> { match });
    _matchMatcher.FindBestMatch("Arsenal", "Chelsea", Arg.Any<IReadOnlyList<(string HomeName, string AwayName, Match Value)>>()).Returns(match);
    _unitOfWork.Matches.GetLineup(10).Returns((Lineup?)null);

    await _sut.Handle(new UpdateLineupsCommand(SupportedLeagueId), CancellationToken.None);

    await _unitOfWork.Matches.Received(1).AddLineup(Arg.Is<Lineup>(l => l.MatchId == 10 && !string.IsNullOrEmpty(l.HomeTeamJson) && !string.IsNullOrEmpty(l.AwayTeamJson)));
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenOneMatchFound_ExistingLineup_UpdatesJsonAndSaveChanges()
  {
    SetupInWindowSeason();
    var date = new DateTime(2026, 1, 15, 14, 0, 0, DateTimeKind.Utc);
    var lineup = CreateLineup(date);
    _lineupProvider.GetSoccerLineupsAsync(SupportedLeagueSlug, Arg.Any<CancellationToken>())
      .Returns(new[] { lineup });

    var match = new Match { Id = 10, HomeClub = new ClubEntity { Name = "Arsenal" }, AwayClub = new ClubEntity { Name = "Chelsea" } };
    _unitOfWork.Matches.GetMatches(date).Returns(new List<Match> { match });
    _matchMatcher.FindBestMatch("Arsenal", "Chelsea", Arg.Any<IReadOnlyList<(string HomeName, string AwayName, Match Value)>>()).Returns(match);

    var existingLineup = new Lineup { MatchId = 10, HomeTeamJson = "old", AwayTeamJson = "old" };
    _unitOfWork.Matches.GetLineup(10).Returns(existingLineup);

    await _sut.Handle(new UpdateLineupsCommand(SupportedLeagueId), CancellationToken.None);

    existingLineup.HomeTeamJson.Should().NotBe("old");
    existingLineup.AwayTeamJson.Should().NotBe("old");
    await _unitOfWork.Matches.DidNotReceive().AddLineup(Arg.Any<Lineup>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
