using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Clubs.GetClubBetSelectionStats;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Clubs;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Clubs.GetClubBetSelectionStats;

public class GetClubBetSelectionStatsHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IClubRepository _clubs = Substitute.For<IClubRepository>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();
  private readonly GetClubBetSelectionStatsHandler _sut;

  public GetClubBetSelectionStatsHandlerTests()
  {
    _unitOfWork.Clubs.Returns(_clubs);
    _unitOfWork.Betting.Returns(_betting);
    _sut = new GetClubBetSelectionStatsHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenClubMissing_ReturnsNull()
  {
    _clubs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((ClubEntity?)null);

    var result = await _sut.Handle(new GetClubBetSelectionStatsQuery(1), CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task Handle_WhenClubExists_ReturnsStatsFromRepository()
  {
    const int clubId = 7;
    _clubs.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new ClubEntity { Id = clubId, Name = "Club" });
    _betting
      .GetBettingPhaseSettledSelectionStatsForClubAsync(clubId, Arg.Any<CancellationToken>())
      .Returns(new ClubBetSelectionStats(3, 2, 5));

    var result = await _sut.Handle(new GetClubBetSelectionStatsQuery(clubId), CancellationToken.None);

    result.Should().NotBeNull();
    result!.WonCount.Should().Be(3);
    result.LostCount.Should().Be(2);
    result.TotalCount.Should().Be(5);
  }
}
