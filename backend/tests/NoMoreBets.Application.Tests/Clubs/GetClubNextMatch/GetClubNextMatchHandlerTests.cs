using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Clubs.GetClubNextMatch;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Matches;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using DomainMatch = NoMoreBets.Domain.Matches.Match;

namespace NoMoreBets.Application.Tests.Clubs.GetClubNextMatch;

public class GetClubNextMatchHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IClubRepository _clubs = Substitute.For<IClubRepository>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly GetClubNextMatchHandler _sut;

  public GetClubNextMatchHandlerTests()
  {
    _unitOfWork.Clubs.Returns(_clubs);
    _unitOfWork.Matches.Returns(_matches);
    _sut = new GetClubNextMatchHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenClubMissing_ReturnsNull()
  {
    _clubs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((ClubEntity?)null);

    var result = await _sut.Handle(new GetClubNextMatchQuery(1), CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task Handle_WhenNoUpcomingMatch_ReturnsNull()
  {
    _clubs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new ClubEntity { Id = 1, Name = "A" });
    _matches.GetNextUpcomingMatchForClubAsync(1, Arg.Any<CancellationToken>()).Returns((DomainMatch?)null);

    var result = await _sut.Handle(new GetClubNextMatchQuery(1), CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task Handle_WhenUpcomingMatchExists_MapsIsHome()
  {
    const int clubId = 10;
    _clubs.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new ClubEntity { Id = clubId, Name = "Home FC" });
    var match = new DomainMatch
    {
      Id = 100,
      MatchDate = new DateTime(2026, 6, 1, 15, 0, 0, DateTimeKind.Utc),
      HomeClubId = clubId,
      AwayClubId = 20,
      HomeClub = new ClubEntity { Id = clubId, Name = "Home FC", Slug = "home-fc" },
      AwayClub = new ClubEntity { Id = 20, Name = "Away FC", Slug = "away-fc" },
    };
    _matches.GetNextUpcomingMatchForClubAsync(clubId, Arg.Any<CancellationToken>()).Returns(match);

    var result = await _sut.Handle(new GetClubNextMatchQuery(clubId), CancellationToken.None);

    result.Should().NotBeNull();
    result!.MatchId.Should().Be(100);
    result.IsHome.Should().BeTrue();
    result.HomeClubName.Should().Be("Home FC");
    result.AwayClubName.Should().Be("Away FC");
  }
}
