using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Clubs;
using NoMoreBets.Application.Clubs.GetOverview;
using NoMoreBets.Application.Common.Dto.Leagues;

namespace NoMoreBets.Application.Tests.Club.GetOverview;

public class GetClubOverviewHandlerTests
{
  private readonly IClubOverviewProvider _provider = Substitute.For<IClubOverviewProvider>();
  private readonly GetClubOverviewHandler _sut;

  public GetClubOverviewHandlerTests()
  {
    _sut = new GetClubOverviewHandler(_provider);
  }

  [Fact]
  public async Task Handle_DelegatesToProvider_WithFotmobClubId()
  {
    // Arrange
    var expected = new ClubOverview { RecentGames = [], DailySummary = "ok" };
    _provider.GetClubOverviewAsync(9876, Arg.Any<CancellationToken>())
      .Returns(expected);

    // Act
    var result = await _sut.Handle(new GetClubOverviewQuery(9876), CancellationToken.None);

    // Assert
    result.Should().BeSameAs(expected);
    await _provider.Received(1).GetClubOverviewAsync(9876, Arg.Any<CancellationToken>());
  }
}
