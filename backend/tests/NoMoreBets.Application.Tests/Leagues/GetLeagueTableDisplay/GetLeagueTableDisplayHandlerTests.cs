using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues.GetLeagueTableDisplay;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Leagues.GetLeagueTableDisplay;

public class GetLeagueTableDisplayHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly ILeagueRepository _leagues = Substitute.For<ILeagueRepository>();
  private readonly GetLeagueTableDisplayHandler _sut;

  public GetLeagueTableDisplayHandlerTests()
  {
    _unitOfWork.Leagues.Returns(_leagues);
    _sut = new GetLeagueTableDisplayHandler(_unitOfWork);
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
    result.Rows[1].Position.Should().Be(2);
    result.Rows[1].ClubName.Should().Be("Barcelona");
    result.Rows[1].Points.Should().Be(59);
  }
}
