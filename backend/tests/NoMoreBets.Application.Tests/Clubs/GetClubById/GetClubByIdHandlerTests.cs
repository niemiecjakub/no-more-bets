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
      LeagueId = 1,
      League = new League { Id = 1, Name = "Premier League", Slug = "premier-league" },
    };
    _clubs.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(club);

    var result = await _sut.Handle(new GetClubByIdQuery(5), CancellationToken.None);

    result.Should().NotBeNull();
    result!.Id.Should().Be(5);
    result.Name.Should().Be("Arsenal");
    result.LeagueName.Should().Be("Premier League");
    result.LeagueSlug.Should().Be("premier-league");
  }
}
