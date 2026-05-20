using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Clubs.GetClubMatches;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchesReadyForPrediction;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Matches;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Application.Tests.Clubs.GetClubMatches;

public class GetClubMatchesHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IClubRepository _clubRepository = Substitute.For<IClubRepository>();
  private readonly IMatchRepository _matchRepository = Substitute.For<IMatchRepository>();
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly GetClubMatchesHandler _sut;

  public GetClubMatchesHandlerTests()
  {
    _unitOfWork.Clubs.Returns(_clubRepository);
    _unitOfWork.Matches.Returns(_matchRepository);
    _sut = new GetClubMatchesHandler(_unitOfWork, _mediator);
  }

  [Fact]
  public async Task Handle_WhenClubNotFound_ReturnsNull()
  {
    // Arrange
    _clubRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((ClubEntity?)null);

    // Act
    var result = await _sut.Handle(new GetClubMatchesQuery(1), CancellationToken.None);

    // Assert
    result.Should().BeNull();
    await _matchRepository.DidNotReceive().GetMatchesForClubAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenNoMatches_ReturnsEmptyList()
  {
    // Arrange
    const int clubId = 3;
    _clubRepository.GetByIdAsync(clubId, Arg.Any<CancellationToken>()).Returns(new ClubEntity { Id = clubId, Name = "Club A" });
    _matchRepository.GetMatchesForClubAsync(clubId, Arg.Any<CancellationToken>()).Returns(new List<Match>());
    _mediator
      .Send(Arg.Any<GetUpcomingMatchesReadyForPredictionQuery>(), Arg.Any<CancellationToken>())
      .Returns(new List<Match>());

    // Act
    var result = await _sut.Handle(new GetClubMatchesQuery(clubId), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
  }
}
