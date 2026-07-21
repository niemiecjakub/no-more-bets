using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues.GetClubLeagueStatistics;
using NoMoreBets.Domain.Clubs;

namespace NoMoreBets.Application.Tests.Leagues.GetClubLeagueStatistics;

public class GetClubLeagueStatisticsHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IClubRepository _clubRepository = Substitute.For<IClubRepository>();
  private readonly GetClubLeagueStatisticsHandler _sut;

  public GetClubLeagueStatisticsHandlerTests()
  {
    _unitOfWork.Clubs.Returns(_clubRepository);
    _sut = new GetClubLeagueStatisticsHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WithDate_PassesDateToRepository()
  {
    // Arrange
    const int clubId = 5;
    var date = new DateOnly(2026, 3, 20);
    _clubRepository.GetCurrentClubLeagueStatsAsync(clubId, date, null, Arg.Any<CancellationToken>())
      .Returns((ClubLeagueStats?)null);

    // Act
    await _sut.Handle(new GetClubLeagueStatisticsQuery(clubId, date), CancellationToken.None);

    // Assert
    await _clubRepository.Received(1).GetCurrentClubLeagueStatsAsync(clubId, date, null, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WithoutDate_PassesNullDateToRepository()
  {
    // Arrange
    const int clubId = 6;
    _clubRepository.GetCurrentClubLeagueStatsAsync(clubId, null, null, Arg.Any<CancellationToken>())
      .Returns((ClubLeagueStats?)null);

    // Act
    await _sut.Handle(new GetClubLeagueStatisticsQuery(clubId), CancellationToken.None);

    // Assert
    await _clubRepository.Received(1).GetCurrentClubLeagueStatsAsync(clubId, null, null, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WithSeasonId_PassesSeasonIdToRepository()
  {
    // Arrange
    const int clubId = 7;
    const int seasonId = 42;
    var date = new DateOnly(2026, 3, 20);
    _clubRepository.GetCurrentClubLeagueStatsAsync(clubId, date, seasonId, Arg.Any<CancellationToken>())
      .Returns((ClubLeagueStats?)null);

    // Act
    await _sut.Handle(new GetClubLeagueStatisticsQuery(clubId, date, seasonId), CancellationToken.None);

    // Assert
    await _clubRepository.Received(1).GetCurrentClubLeagueStatsAsync(clubId, date, seasonId, Arg.Any<CancellationToken>());
  }
}
