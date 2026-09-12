using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues.GetLeaguesList;
using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Application.Tests.Leagues.GetLeaguesList;

public class GetLeaguesListHandlerTests
{
  private readonly ILeagueRepository _leagues = Substitute.For<ILeagueRepository>();
  private readonly GetLeaguesListHandler _sut;

  public GetLeaguesListHandlerTests()
  {
    _sut = new GetLeaguesListHandler(_leagues);
  }

  [Fact]
  public async Task Handle_DelegatesToRepository_AndMapsLeagueDto()
  {
    IReadOnlyList<League> list =
    [
      new() { Id = 2, Name = "Bundesliga", Slug = "bundesliga" },
      new() { Id = 1, Name = "Premier League", Slug = "premier-league" },
    ];
    _leagues.GetLeaguesOrderedByNameAsync(Arg.Any<CancellationToken>()).Returns(list);

    var result = await _sut.Handle(new GetLeaguesListQuery(), CancellationToken.None);

    await _leagues.Received(1).GetLeaguesOrderedByNameAsync(Arg.Any<CancellationToken>());
    result.Should().HaveCount(2);
    result[0].Id.Should().Be(2);
    result[0].Name.Should().Be("Bundesliga");
    result[0].Slug.Should().Be("bundesliga");
    result[1].Id.Should().Be(1);
  }
}
