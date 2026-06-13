using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues;
using NoMoreBets.Application.Leagues.GetLeagueTableDisplay;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Leagues.GetLeagueTableDisplay;

public class GetLeagueTableDisplayHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly ILeagueRepository _leagues = Substitute.For<ILeagueRepository>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly IClubRepository _clubs = Substitute.For<IClubRepository>();
  private readonly WorldCupGroupRegistry _worldCupGroupRegistry = new([]);
  private readonly GetLeagueTableDisplayHandler _sut;

  public GetLeagueTableDisplayHandlerTests()
  {
    _unitOfWork.Leagues.Returns(_leagues);
    _unitOfWork.Matches.Returns(_matches);
    _unitOfWork.Clubs.Returns(_clubs);
    _sut = new GetLeagueTableDisplayHandler(_unitOfWork, _worldCupGroupRegistry);
  }

  [Fact]
  public async Task Handle_WhenSnapshotMissing_ReturnsNull()
  {
    _leagues
      .GetLatestLeagueTableSnapshotAsync(9, Arg.Any<CancellationToken>())
      .Returns((LeagueTableSnapshot?)null);

    var result = await _sut.Handle(new GetLeagueTableDisplayQuery(9), CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task Handle_MapsSnapshotMetadataAndRows()
  {
    var snapshot = new LeagueTableSnapshot
    {
      Id = 100,
      LeagueId = 3,
      SeasonId = 7,
      SnapshotDate = new DateOnly(2026, 3, 15),
      League = new League { Name = "La Liga", Slug = "la-liga" },
      Rows =
      [
        new LeagueTableSnapshotRow
        {
          Position = 2,
          ClubId = 20,
          Club = new ClubEntity { Name = "Barcelona", Slug = "barcelona" },
          MatchesPlayed = 28,
          Wins = 18,
          Draws = 5,
          Losses = 5,
          GoalsFor = 60,
          GoalsAgainst = 30,
          GoalDifference = 30,
          Points = 59,
          Xg = 55.1m,
          XgDiff = 2.1m,
          Xga = 28.0m,
          XgaDiff = -1.0m,
          Xpts = 58.0m,
          XptsDiff = 1.0m,
        },
        new LeagueTableSnapshotRow
        {
          Position = 1,
          ClubId = 10,
          Club = new ClubEntity { Name = "Real Madrid", Slug = "real-madrid" },
          MatchesPlayed = 28,
          Wins = 20,
          Draws = 4,
          Losses = 4,
          GoalsFor = 65,
          GoalsAgainst = 25,
          GoalDifference = 40,
          Points = 64,
          Xg = 60.0m,
          XgDiff = 3.0m,
          Xga = 25.0m,
          XgaDiff = -2.0m,
          Xpts = 62.0m,
          XptsDiff = 2.0m,
        },
      ],
    };
    _leagues
      .GetLatestLeagueTableSnapshotAsync(3, Arg.Any<CancellationToken>())
      .Returns(snapshot);

    var formByClub = new Dictionary<int, IReadOnlyList<MatchResult>>
    {
      [10] = [MatchResult.Win, MatchResult.Win, MatchResult.Draw, MatchResult.Loss, MatchResult.Win],
      [20] = [MatchResult.Loss, MatchResult.Win, MatchResult.Win],
    };
    _matches
      .GetFormForClubsInSeasonAsync(7, Arg.Any<IReadOnlyList<int>>(), 5, Arg.Any<CancellationToken>())
      .Returns(formByClub);

    var result = await _sut.Handle(new GetLeagueTableDisplayQuery(3), CancellationToken.None);

    result.Should().NotBeNull();
    result!.SnapshotId.Should().Be(100);
    result.LeagueId.Should().Be(3);
    result.SeasonId.Should().Be(7);
    result.SnapshotDate.Should().Be(new DateOnly(2026, 3, 15));
    result.LeagueName.Should().Be("La Liga");
    result.LeagueSlug.Should().Be("la-liga");
    result.Rows.Should().HaveCount(2);
    result.Rows[0].Position.Should().Be(1);
    result.Rows[0].ClubName.Should().Be("Real Madrid");
    result.Rows[0].Form.Should().Equal(
      MatchResult.Win,
      MatchResult.Win,
      MatchResult.Draw,
      MatchResult.Loss,
      MatchResult.Win);
    result.Rows[1].Position.Should().Be(2);
    result.Rows[1].ClubName.Should().Be("Barcelona");
    result.Rows[1].Points.Should().Be(59);
    result.Rows[1].Form.Should().Equal(MatchResult.Loss, MatchResult.Win, MatchResult.Win);

    await _matches.Received(1).GetFormForClubsInSeasonAsync(
      7,
      Arg.Is<IReadOnlyList<int>>(ids => ids.OrderBy(x => x).SequenceEqual(new[] { 10, 20 })),
      5,
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenNoFormData_ReturnsEmptyFormLists()
  {
    var snapshot = new LeagueTableSnapshot
    {
      Id = 1,
      LeagueId = 1,
      SeasonId = 2,
      SnapshotDate = new DateOnly(2026, 1, 1),
      League = new League { Name = "PL", Slug = "pl" },
      Rows =
      [
        new LeagueTableSnapshotRow
        {
          Position = 1,
          ClubId = 5,
          Club = new ClubEntity { Name = "Arsenal", Slug = "arsenal" },
          MatchesPlayed = 10,
          Wins = 7,
          Draws = 2,
          Losses = 1,
          Points = 23,
        },
      ],
    };
    _leagues
      .GetLatestLeagueTableSnapshotAsync(1, Arg.Any<CancellationToken>())
      .Returns(snapshot);
    _matches
      .GetFormForClubsInSeasonAsync(2, Arg.Any<IReadOnlyList<int>>(), 5, Arg.Any<CancellationToken>())
      .Returns(new Dictionary<int, IReadOnlyList<MatchResult>>());

    var result = await _sut.Handle(new GetLeagueTableDisplayQuery(1), CancellationToken.None);

    result!.Rows[0].Form.Should().BeEmpty();
  }

  [Fact]
  public async Task Handle_WhenWorldCupClubIdProvided_ReturnsGroupedTablesWithOwnGroupFirst()
  {
    var groupA = new WorldCupGroupDefinition("A", "Grp. A", [6710, 7804, 8496, 6316], ["Mexico", "Korea Republic", "Czechia", "South Africa"]);
    var groupB = new WorldCupGroupDefinition("B", "Grp. B", [6717, 5810, 10106, 5902], ["Switzerland", "Canada", "Bosnia-Herzegovina", "Qatar"]);
    var sut = new GetLeagueTableDisplayHandler(_unitOfWork, new WorldCupGroupRegistry([groupA, groupB]));

    var club = new ClubEntity { Id = 1, Name = "Mexico", LeagueId = 7, Slug = "mexico" };
    _clubs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(club);

    var snapshot = new LeagueTableSnapshot
    {
      Id = 50,
      LeagueId = 7,
      SeasonId = 7,
      SnapshotDate = new DateOnly(2026, 6, 1),
      League = new League { Name = "FIFA World Cup", Slug = League.FifaWorldCupSlug },
      Rows =
      [
        new LeagueTableSnapshotRow
        {
          Position = 1,
          ClubId = 1,
          Club = new ClubEntity { Name = "Mexico", Slug = "mexico" },
          MatchesPlayed = 1,
          Wins = 1,
          Draws = 0,
          Losses = 0,
          Points = 3,
        },
        new LeagueTableSnapshotRow
        {
          Position = 2,
          ClubId = 2,
          Club = new ClubEntity { Name = "Korea Republic", Slug = "korea-republic" },
          MatchesPlayed = 1,
          Wins = 1,
          Draws = 0,
          Losses = 0,
          Points = 3,
        },
        new LeagueTableSnapshotRow
        {
          Position = 1,
          ClubId = 3,
          Club = new ClubEntity { Name = "Switzerland", Slug = "switzerland" },
          MatchesPlayed = 1,
          Wins = 0,
          Draws = 1,
          Losses = 0,
          Points = 1,
        },
        new LeagueTableSnapshotRow
        {
          Position = 2,
          ClubId = 4,
          Club = new ClubEntity { Name = "Canada", Slug = "canada" },
          MatchesPlayed = 1,
          Wins = 0,
          Draws = 0,
          Losses = 1,
          Points = 0,
        },
      ],
    };

    _leagues.GetLatestLeagueTableSnapshotAsync(7, Arg.Any<CancellationToken>()).Returns(snapshot);
    _matches.GetFormForClubsInSeasonAsync(7, Arg.Any<IReadOnlyList<int>>(), 5, Arg.Any<CancellationToken>())
      .Returns(new Dictionary<int, IReadOnlyList<MatchResult>>());

    var result = await sut.Handle(new GetLeagueTableDisplayQuery(7, 1), CancellationToken.None);

    result.Should().NotBeNull();
    result!.OwnGroupCode.Should().Be("A");
    result.Groups.Should().HaveCount(2);
    result.Groups![0].GroupCode.Should().Be("A");
    result.Groups[0].Rows.Should().HaveCount(2);
    result.Groups[1].GroupCode.Should().Be("B");
    result.Rows.Should().Equal(result.Groups[0].Rows);
  }
}
