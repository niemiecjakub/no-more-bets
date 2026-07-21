using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Clubs.GetClubById;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Clubs.GetClubById;

public class GetClubByIdHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IClubRepository _clubs = Substitute.For<IClubRepository>();
  private readonly GetClubByIdHandler _sut;

  public GetClubByIdHandlerTests()
  {
    _unitOfWork.Clubs.Returns(_clubs);
    _sut = new GetClubByIdHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenClubMissing_ReturnsNull()
  {
    _clubs.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((ClubEntity?)null);

    var result = await _sut.Handle(new GetClubByIdQuery(99), CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task Handle_WhenClubExists_ReturnsDetailDto()
  {
    var club = new ClubEntity
    {
      Id = 5,
      Name = "Arsenal",
      Slug = "arsenal",
      ClubSeasons =
      [
        new ClubSeason
        {
          SeasonId = 1,
          Season = new Season
          {
            Id = 1,
            LeagueId = 1,
            Year = "2025-2026",
            StartDate = new DateOnly(2025, 8, 15),
            League = new League { Id = 1, Name = "Premier League", Slug = "premier-league" }
          }
        },
        new ClubSeason
        {
          SeasonId = 9,
          Season = new Season
          {
            Id = 9,
            LeagueId = 2,
            Year = "2026-2027",
            StartDate = new DateOnly(2026, 7, 24),
            League = new League { Id = 2, Name = "Ekstraklasa", Slug = "ekstraklasa" }
          }
        },
        new ClubSeason
        {
          SeasonId = 3,
          Season = new Season
          {
            Id = 3,
            LeagueId = 99,
            Year = "2024-2025",
            StartDate = new DateOnly(2024, 8, 1),
            League = new League { Id = 99, Name = "Unknown", Slug = League.UnknownSlug }
          }
        }
      ]
    };
    _clubs.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(club);

    var result = await _sut.Handle(new GetClubByIdQuery(5), CancellationToken.None);

    result.Should().NotBeNull();
    result!.Id.Should().Be(5);
    result.Name.Should().Be("Arsenal");
    result.Memberships.Should().HaveCount(2);
    result.Memberships[0].SeasonId.Should().Be(9);
    result.Memberships[0].LeagueName.Should().Be("Ekstraklasa");
    result.Memberships[1].LeagueName.Should().Be("Premier League");
  }
}
