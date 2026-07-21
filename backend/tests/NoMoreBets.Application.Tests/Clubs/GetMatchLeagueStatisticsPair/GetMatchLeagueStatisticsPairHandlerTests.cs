using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Clubs.GetMatchLeagueStatisticsPair;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues.GetClubLeagueStatistics;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Clubs.GetMatchLeagueStatisticsPair;

public class GetMatchLeagueStatisticsPairHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly GetMatchLeagueStatisticsPairHandler _sut;

  public GetMatchLeagueStatisticsPairHandlerTests()
  {
    _unitOfWork.Matches.Returns(_matches);
    _sut = new GetMatchLeagueStatisticsPairHandler(_unitOfWork, _mediator);
  }

  [Fact]
  public async Task Handle_WhenMatchMissing_ReturnsNull()
  {
    // Arrange
    _matches.GetMatchByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Match?)null);

    // Act
    var result = await _sut.Handle(new GetMatchLeagueStatisticsPairQuery(99), CancellationToken.None);

    // Assert
    result.Should().BeNull();
    await _mediator.DidNotReceive().Send(Arg.Any<GetClubLeagueStatisticsQuery>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenMatchHasStage_PassesMatchSeasonIdToQueries()
  {
    // Arrange
    const int seasonId = 42;
    var match = new Match
    {
      Id = 7,
      HomeClubId = 1,
      AwayClubId = 2,
      MatchDate = new DateTime(2026, 3, 20, 15, 0, 0, DateTimeKind.Utc),
      Stage = new Stage { Id = 3, SeasonId = seasonId, Name = "Regular" },
    };
    _matches.GetMatchByIdAsync(7, Arg.Any<CancellationToken>()).Returns(match);
    _mediator.Send(Arg.Any<GetClubLeagueStatisticsQuery>(), Arg.Any<CancellationToken>())
      .Returns((ClubLeagueStats?)null);

    // Act
    await _sut.Handle(new GetMatchLeagueStatisticsPairQuery(7), CancellationToken.None);

    // Assert
    var expectedDate = new DateOnly(2026, 3, 20);
    await _mediator.Received(1).Send(
      Arg.Is<GetClubLeagueStatisticsQuery>(q => q.ClubId == 1 && q.Date == expectedDate && q.SeasonId == seasonId),
      Arg.Any<CancellationToken>());
    await _mediator.Received(1).Send(
      Arg.Is<GetClubLeagueStatisticsQuery>(q => q.ClubId == 2 && q.Date == expectedDate && q.SeasonId == seasonId),
      Arg.Any<CancellationToken>());
  }
}
