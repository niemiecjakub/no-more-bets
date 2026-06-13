using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues;
using NoMoreBets.Application.Leagues.GetMatchGroupTable;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Leagues.GetMatchGroupTable;

public class GetMatchGroupTableHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly ILeagueRepository _leagues = Substitute.For<ILeagueRepository>();
  private readonly WorldCupGroupRegistry _worldCupGroupRegistry = new(
  [
    new("A", "Grp. A", [6710, 7804, 8496, 6316], ["Mexico", "Korea Republic", "Czechia", "South Africa"]),
    new("B", "Grp. B", [6717, 5810, 10106, 5902], ["Switzerland", "Canada", "Bosnia-Herzegovina", "Qatar"]),
  ]);
  private readonly GetMatchGroupTableHandler _sut;

  public GetMatchGroupTableHandlerTests()
  {
    _unitOfWork.Matches.Returns(_matches);
    _unitOfWork.Leagues.Returns(_leagues);
    _sut = new GetMatchGroupTableHandler(_unitOfWork, _worldCupGroupRegistry);
  }

  [Fact]
  public async Task Handle_WhenWorldCupMatch_ReturnsStandingsForHomeClubGroupOnly()
  {
    var league = new League { Id = 7, Name = "FIFA World Cup", Slug = League.FifaWorldCupSlug, SoccerdataId = 313 };
    var season = new Season { Id = 7, LeagueId = 7, Year = "2026", League = league };
    var stage = new Stage { Id = 7, SeasonId = 7, Name = "World Championship", Season = season };
    var match = new Match
    {
      Id = 10,
      HomeClub = new ClubEntity { Id = 1, Name = "Mexico", Slug = "mexico" },
      AwayClub = new ClubEntity { Id = 2, Name = "Korea Republic", Slug = "korea-republic" },
      Stage = stage,
    };
    _matches.GetMatchByIdAsync(10, Arg.Any<CancellationToken>()).Returns(match);

    var standings = new List<LeagueTableStanding>
    {
      new(1, "Mexico", Stats(1, 3)),
      new(2, "Korea Republic", Stats(2, 3)),
      new(3, "Switzerland", Stats(1, 1)),
      new(4, "Canada", Stats(2, 0)),
    };
    _leagues.GetLeagueTableAsOfAsync(7, null, Arg.Any<CancellationToken>()).Returns(standings);

    var result = await _sut.Handle(new GetMatchGroupTableQuery(10), CancellationToken.None);

    result.Should().NotBeNull();
    result!.Select(s => s.ClubName).Should().Equal("Mexico", "Korea Republic");
  }

  [Fact]
  public async Task Handle_WhenNotWorldCupMatch_ReturnsNull()
  {
    var league = new League { Id = 1, Name = "Premier League", Slug = "premier-league", SoccerdataId = 228 };
    var season = new Season { Id = 1, LeagueId = 1, Year = "2025", League = league };
    var stage = new Stage { Id = 1, SeasonId = 1, Name = "PL", Season = season };
    var match = new Match
    {
      Id = 11,
      HomeClub = new ClubEntity { Id = 1, Name = "Arsenal", Slug = "arsenal" },
      AwayClub = new ClubEntity { Id = 2, Name = "Chelsea", Slug = "chelsea" },
      Stage = stage,
    };
    _matches.GetMatchByIdAsync(11, Arg.Any<CancellationToken>()).Returns(match);

    var result = await _sut.Handle(new GetMatchGroupTableQuery(11), CancellationToken.None);

    result.Should().BeNull();
    await _leagues.DidNotReceive().GetLeagueTableAsOfAsync(Arg.Any<int>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>());
  }

  private static ClubLeagueStats Stats(int position, int points) =>
    new(new LeagueTableSnapshotRow
    {
      Position = position,
      Points = points,
      ClubId = 1,
    });
}
