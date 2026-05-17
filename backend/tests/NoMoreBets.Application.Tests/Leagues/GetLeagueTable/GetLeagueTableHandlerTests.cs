using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues.GetLeagueTable;
using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Application.Tests.Leagues.GetLeagueTable;

public class GetLeagueTableHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly ILeagueRepository _leagues = Substitute.For<ILeagueRepository>();
  private readonly GetLeagueTableHandler _sut;

  public GetLeagueTableHandlerTests()
  {
    _unitOfWork.Leagues.Returns(_leagues);
    _sut = new GetLeagueTableHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_DelegatesToLeagueRepository_WithLeagueIdAndAsOfDate()
  {
    // Arrange
    var asOf = new DateOnly(2026, 3, 10);
    IReadOnlyList<LeagueTableStanding>? table = [];
    _leagues.GetLeagueTableAsOfAsync(7, asOf, Arg.Any<CancellationToken>())
      .Returns(table);

    // Act
    var result = await _sut.Handle(new GetLeagueTableQuery(7, asOf), CancellationToken.None);

    // Assert
    result.Should().BeSameAs(table);
    await _leagues.Received(1).GetLeagueTableAsOfAsync(7, asOf, Arg.Any<CancellationToken>());
  }
}
