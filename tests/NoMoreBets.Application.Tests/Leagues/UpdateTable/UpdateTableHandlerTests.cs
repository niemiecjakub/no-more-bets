using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto.Leagues;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Application.Leagues;
using NoMoreBets.Application.Leagues.UpdateTable;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Leagues.UpdateTable;

public class UpdateTableHandlerTests
{
  private readonly ILeagueProvider _leagueProvider;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IMatchMatcher _matchMatcher;
  private readonly ILogger<UpdateTableHandler> _logger;
  private readonly UpdateTableHandler _sut;

  public UpdateTableHandlerTests()
  {
    _leagueProvider = Substitute.For<ILeagueProvider>();
    _unitOfWork = Substitute.For<IUnitOfWork>();
    _matchMatcher = Substitute.For<IMatchMatcher>();
    _logger = Substitute.For<ILogger<UpdateTableHandler>>();
    _sut = new UpdateTableHandler(_leagueProvider, _unitOfWork, _matchMatcher, _logger);
  }

  [Fact]
  public async Task Handle_WhenNoSeasonForLeague_ThrowsInvalidOperationException()
  {
    _unitOfWork.Leagues.GetLatestSeason(Arg.Any<int>()).Returns((Season?)null);

    var act = () => _sut.Handle(new UpdateTableCommand(42), CancellationToken.None);

    (await act.Should().ThrowAsync<InvalidOperationException>())
      .WithMessage("*42*");
    await _leagueProvider.DidNotReceive().GetLeagueTableAsync(Arg.Any<CancellationToken>());
    await _leagueProvider.DidNotReceive().GetXgStatsAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenSnapshotAlreadyExists_ReturnsWithoutCallingProvider()
  {
    _unitOfWork.Leagues.GetLatestSeason(Arg.Any<int>()).Returns(new Season { Id = 1, LeagueId = 1, Year = "2025" });
    _unitOfWork.Leagues.TableSnapshotExists(Arg.Any<int>(), Arg.Any<DateOnly>()).Returns(true);

    var result = await _sut.Handle(new UpdateTableCommand(1), CancellationToken.None);

    result.Should().Be(Unit.Value);
    await _leagueProvider.DidNotReceive().GetLeagueTableAsync(Arg.Any<CancellationToken>());
    await _leagueProvider.DidNotReceive().GetXgStatsAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenFindClubThrowsForOneTableClub_PropagatesException()
  {
    _unitOfWork.Leagues.GetLatestSeason(Arg.Any<int>()).Returns(new Season { Id = 1, LeagueId = 1, Year = "2025" });
    _unitOfWork.Leagues.TableSnapshotExists(Arg.Any<int>(), Arg.Any<DateOnly>()).Returns(false);
    var domainClubs = new List<ClubEntity> { new() { Id = 1, Name = "Arsenal", LeagueId = 1, SoccerdataId = 1 } };
    _unitOfWork.Clubs.GetClubs(Arg.Any<int>()).Returns(Task.FromResult(domainClubs));

    var tableEntry = new TableEntry
    {
      TeamName = "Unknown Team",
      Position = 1,
      MatchesPlayed = 10,
      Wins = 5,
      Draws = 2,
      Losses = 3,
      GoalsFor = 15,
      GoalsAgainst = 10,
      GoalDifference = "5",
      Points = 17
    };
    _leagueProvider.GetLeagueTableAsync(Arg.Any<CancellationToken>()).Returns(new[] { tableEntry });
    _leagueProvider.GetXgStatsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<XgStats>());

    _matchMatcher.FindClub("Unknown Team", Arg.Any<IReadOnlyList<ClubEntity>>())
      .Returns(_ => throw new InvalidOperationException("No matching club"));

    var act = () => _sut.Handle(new UpdateTableCommand(1), CancellationToken.None);

    await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No matching club*");
    await _unitOfWork.Leagues.DidNotReceive().AddLeagueTableSnapshot(Arg.Any<LeagueTableSnapshot>());
  }

  [Fact]
  public async Task Handle_WhenAllMatchesPlayedUnchanged_SkipsSnapshotCreation()
  {
    _unitOfWork.Leagues.GetLatestSeason(Arg.Any<int>()).Returns(new Season { Id = 1, LeagueId = 1, Year = "2025" });
    _unitOfWork.Leagues.TableSnapshotExists(Arg.Any<int>(), Arg.Any<DateOnly>()).Returns(false);

    var club = new ClubEntity { Id = 1, Name = "Arsenal", LeagueId = 1, SoccerdataId = 1 };
    _unitOfWork.Clubs.GetClubs(Arg.Any<int>()).Returns(Task.FromResult(new List<ClubEntity> { club }));

    var tableEntry = new TableEntry
    {
      TeamName = "Arsenal",
      Position = 1,
      MatchesPlayed = 10,
      Wins = 5,
      Draws = 2,
      Losses = 3,
      GoalsFor = 15,
      GoalsAgainst = 10,
      GoalDifference = "5",
      Points = 17
    };
    _leagueProvider.GetLeagueTableAsync(Arg.Any<CancellationToken>()).Returns(new[] { tableEntry });
    _leagueProvider.GetXgStatsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<XgStats>());

    var latestSnapshot = new LeagueTableSnapshot { Id = 1, LeagueId = 1, SeasonId = 1 };
    latestSnapshot.Rows.Add(new LeagueTableSnapshotRow { ClubId = 1, MatchesPlayed = 10 });
    _unitOfWork.Leagues.GetLatestTableSnapshot(Arg.Any<int>()).Returns(latestSnapshot);

    _matchMatcher.FindClub("Arsenal", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(club);

    var result = await _sut.Handle(new UpdateTableCommand(1), CancellationToken.None);

    result.Should().Be(Unit.Value);
    await _unitOfWork.Leagues.DidNotReceive().AddLeagueTableSnapshot(Arg.Any<LeagueTableSnapshot>());
  }
}
