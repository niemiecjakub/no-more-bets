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
  public async Task Handle_WhenNoSeasonExists_SkipsUpdate()
  {
    _unitOfWork.Leagues.GetLatestSeasonAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Season?)null);

    var result = await _sut.Handle(new UpdateTableCommand(42), CancellationToken.None);

    result.Should().Be(Unit.Value);
    await _leagueProvider.DidNotReceive().GetLeagueTableAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    await _leagueProvider.DidNotReceive().GetXgStatsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenLatestSeasonOutsideFetchWindow_SkipsUpdate()
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    _unitOfWork.Leagues.GetLatestSeasonAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
      .Returns(new Season
      {
        Id = 1,
        LeagueId = 1,
        Year = "2025",
        StartDate = today.AddDays(14),
        EndDate = today.AddDays(200)
      });

    var result = await _sut.Handle(new UpdateTableCommand(1), CancellationToken.None);

    result.Should().Be(Unit.Value);
    await _leagueProvider.DidNotReceive().GetLeagueTableAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    await _unitOfWork.Leagues.DidNotReceive().TableSnapshotExists(Arg.Any<int>(), Arg.Any<DateOnly>());
  }

  [Theory]
  [InlineData(-7, 100)]  // first day of pre-start window
  [InlineData(0, 100)]   // season start
  [InlineData(-50, 50)]  // mid-season
  [InlineData(-100, 0)]  // season end
  [InlineData(-100, 7)]  // last day of post-end window
  public void SeasonFetchWindow_WhenDateInWindow_ReturnsTrue(int startOffsetDays, int endOffsetDays)
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var season = new Season
    {
      StartDate = today.AddDays(startOffsetDays),
      EndDate = today.AddDays(endOffsetDays)
    };

    SeasonFetchWindow.Contains(season, today).Should().BeTrue();
  }

  [Theory]
  [InlineData(8, 100)]  // more than 7 days before start
  [InlineData(-100, -8)] // more than 7 days after end
  public void SeasonFetchWindow_WhenDateOutsideWindow_ReturnsFalse(int startOffsetDays, int endOffsetDays)
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var season = new Season
    {
      StartDate = today.AddDays(startOffsetDays),
      EndDate = today.AddDays(endOffsetDays)
    };

    SeasonFetchWindow.Contains(season, today).Should().BeFalse();
  }

  [Fact]
  public async Task Handle_WhenSnapshotAlreadyExists_ReturnsWithoutCallingProvider()
  {
    SetupInWindowSeason();
    _unitOfWork.Leagues.TableSnapshotExists(Arg.Any<int>(), Arg.Any<DateOnly>()).Returns(true);

    var result = await _sut.Handle(new UpdateTableCommand(1), CancellationToken.None);

    result.Should().Be(Unit.Value);
    await _leagueProvider.DidNotReceive().GetLeagueTableAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    await _leagueProvider.DidNotReceive().GetXgStatsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenTableDataMissingForSeasonClub_ThrowsWithMissingDetails()
  {
    SetupInWindowSeason();
    _unitOfWork.Leagues.TableSnapshotExists(Arg.Any<int>(), Arg.Any<DateOnly>()).Returns(false);
    _unitOfWork.Leagues.GetLeagues().Returns(new List<League> { new() { Id = 1, Name = "Premier League", Slug = "premier-league", SoccerdataId = 228 } });

    var arsenal = new ClubEntity { Id = 1, Name = "Arsenal", SoccerdataId = 1 };
    var chelsea = new ClubEntity { Id = 2, Name = "Chelsea", SoccerdataId = 2 };
    _unitOfWork.Clubs.GetClubsForSeasonAsync(Arg.Any<int>()).Returns(Task.FromResult(new List<ClubEntity> { arsenal, chelsea }));

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
    _leagueProvider.GetLeagueTableAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new[] { tableEntry });
    _leagueProvider.GetXgStatsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new[]
    {
      new XgStats { TeamName = "Arsenal", Xg = 1.2, Xga = 0.8, Xpts = 1.5 }
    });

    _matchMatcher.FindClub("Arsenal", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(arsenal);
    _matchMatcher.FindXgStats("Arsenal", Arg.Any<IReadOnlyList<XgStats>>())
      .Returns(new XgStats { TeamName = "Arsenal", Xg = 1.2, Xga = 0.8, Xpts = 1.5 });

    var act = () => _sut.Handle(new UpdateTableCommand(1), CancellationToken.None);

    var ex = await act.Should().ThrowAsync<IncompleteLeagueTableDataException>();
    ex.Which.MissingTableDataForClubs.Should().ContainSingle().Which.Should().Be("Chelsea");
    ex.Which.MissingXgDataForClubs.Should().BeEmpty();
    ex.Which.UnmatchedTableTeams.Should().BeEmpty();
    await _unitOfWork.Leagues.DidNotReceive().AddLeagueTableSnapshot(Arg.Any<LeagueTableSnapshot>());
  }

  [Fact]
  public async Task Handle_WhenFindClubThrowsForOneTableClub_ThrowsWithMissingDetails()
  {
    SetupInWindowSeason();
    _unitOfWork.Leagues.TableSnapshotExists(Arg.Any<int>(), Arg.Any<DateOnly>()).Returns(false);
    _unitOfWork.Leagues.GetLeagues().Returns(new List<League> { new() { Id = 1, Name = "Premier League", Slug = "premier-league", SoccerdataId = 228 } });
    var domainClubs = new List<ClubEntity> { new() { Id = 1, Name = "Arsenal", SoccerdataId = 1 } };
    _unitOfWork.Clubs.GetClubsForSeasonAsync(Arg.Any<int>()).Returns(Task.FromResult(domainClubs));

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
    _leagueProvider.GetLeagueTableAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new[] { tableEntry });
    _leagueProvider.GetXgStatsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<XgStats>());

    _matchMatcher.FindClub("Unknown Team", Arg.Any<IReadOnlyList<ClubEntity>>())
      .Returns(_ => throw new ClubMatchNotFoundException("Unknown Team", "No matching club found for 'Unknown Team'."));

    var act = () => _sut.Handle(new UpdateTableCommand(1), CancellationToken.None);

    var ex = await act.Should().ThrowAsync<IncompleteLeagueTableDataException>();
    ex.Which.MissingTableDataForClubs.Should().ContainSingle().Which.Should().Be("Arsenal");
    ex.Which.UnmatchedTableTeams.Should().ContainSingle().Which.Should().Be("Unknown Team");
    await _unitOfWork.Leagues.DidNotReceive().AddLeagueTableSnapshot(Arg.Any<LeagueTableSnapshot>());
  }

  [Fact]
  public async Task Handle_WhenXgDataMissingForSeasonClub_ThrowsWithMissingDetails()
  {
    SetupInWindowSeason();
    _unitOfWork.Leagues.TableSnapshotExists(Arg.Any<int>(), Arg.Any<DateOnly>()).Returns(false);
    _unitOfWork.Leagues.GetLeagues().Returns(new List<League> { new() { Id = 1, Name = "Premier League", Slug = "premier-league", SoccerdataId = 228 } });

    var club = new ClubEntity { Id = 1, Name = "Arsenal", SoccerdataId = 1 };
    _unitOfWork.Clubs.GetClubsForSeasonAsync(Arg.Any<int>()).Returns(Task.FromResult(new List<ClubEntity> { club }));

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
    _leagueProvider.GetLeagueTableAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new[] { tableEntry });
    _leagueProvider.GetXgStatsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<XgStats>());

    _matchMatcher.FindClub("Arsenal", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(club);
    _matchMatcher.FindXgStats("Arsenal", Arg.Any<IReadOnlyList<XgStats>>()).Returns((XgStats?)null);

    var act = () => _sut.Handle(new UpdateTableCommand(1), CancellationToken.None);

    var ex = await act.Should().ThrowAsync<IncompleteLeagueTableDataException>();
    ex.Which.MissingXgDataForClubs.Should().ContainSingle().Which.Should().Be("Arsenal");
    await _unitOfWork.Leagues.DidNotReceive().AddLeagueTableSnapshot(Arg.Any<LeagueTableSnapshot>());
  }

  [Fact]
  public async Task Handle_WhenAllMatchesPlayedUnchanged_SkipsSnapshotCreation()
  {
    SetupInWindowSeason();
    _unitOfWork.Leagues.TableSnapshotExists(Arg.Any<int>(), Arg.Any<DateOnly>()).Returns(false);
    _unitOfWork.Leagues.GetLeagues().Returns(new List<League> { new() { Id = 1, Name = "Premier League", Slug = "premier-league", SoccerdataId = 228 } });

    var club = new ClubEntity { Id = 1, Name = "Arsenal", SoccerdataId = 1 };
    _unitOfWork.Clubs.GetClubsForSeasonAsync(Arg.Any<int>()).Returns(Task.FromResult(new List<ClubEntity> { club }));

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
    _leagueProvider.GetLeagueTableAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new[] { tableEntry });
    _leagueProvider.GetXgStatsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new[]
    {
      new XgStats { TeamName = "Arsenal", Xg = 1.2, Xga = 0.8, Xpts = 1.5 }
    });

    var latestSnapshot = new LeagueTableSnapshot { Id = 1, LeagueId = 1, SeasonId = 1 };
    latestSnapshot.Rows.Add(new LeagueTableSnapshotRow { ClubId = 1, MatchesPlayed = 10 });
    _unitOfWork.Leagues.GetLatestTableSnapshot(Arg.Any<int>()).Returns(latestSnapshot);

    _matchMatcher.FindClub("Arsenal", Arg.Any<IReadOnlyList<ClubEntity>>()).Returns(club);
    _matchMatcher.FindXgStats("Arsenal", Arg.Any<IReadOnlyList<XgStats>>())
      .Returns(new XgStats { TeamName = "Arsenal", Xg = 1.2, Xga = 0.8, Xpts = 1.5 });

    var result = await _sut.Handle(new UpdateTableCommand(1), CancellationToken.None);

    result.Should().Be(Unit.Value);
    await _unitOfWork.Leagues.DidNotReceive().AddLeagueTableSnapshot(Arg.Any<LeagueTableSnapshot>());
  }

  private void SetupInWindowSeason()
  {
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    _unitOfWork.Leagues.GetLatestSeasonAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
      .Returns(new Season
      {
        Id = 1,
        LeagueId = 1,
        Year = "2025",
        StartDate = today.AddDays(-30),
        EndDate = today.AddDays(100)
      });
  }
}
