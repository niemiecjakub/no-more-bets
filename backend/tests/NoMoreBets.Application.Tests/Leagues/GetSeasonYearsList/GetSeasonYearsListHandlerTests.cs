using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues.GetSeasonYearsList;
using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Application.Tests.Leagues.GetSeasonYearsList;

public class GetSeasonYearsListHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly ILeagueRepository _leagues = Substitute.For<ILeagueRepository>();
  private readonly GetSeasonYearsListHandler _sut;

  public GetSeasonYearsListHandlerTests()
  {
    _unitOfWork.Leagues.Returns(_leagues);
    _sut = new GetSeasonYearsListHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_DelegatesToRepository_AndMapsSeasonYearDto()
  {
    _leagues
      .GetSeasonYearsOrderedLatestFirstAsync(Arg.Any<CancellationToken>())
      .Returns(new[] { "2026-2027", "2025-2026", "N/A" });

    var result = await _sut.Handle(new GetSeasonYearsListQuery(), CancellationToken.None);

    await _leagues.Received(1).GetSeasonYearsOrderedLatestFirstAsync(Arg.Any<CancellationToken>());
    result.Should().HaveCount(3);
    result[0].Year.Should().Be("2026-2027");
    result[1].Year.Should().Be("2025-2026");
    result[2].Year.Should().Be("N/A");
  }
}
