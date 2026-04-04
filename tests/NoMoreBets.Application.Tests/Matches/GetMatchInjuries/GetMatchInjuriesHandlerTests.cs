using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchInjuries;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Matches.GetMatchInjuries;

public class GetMatchInjuriesHandlerTests
{
  private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly GetMatchInjuriesHandler _sut;

  public GetMatchInjuriesHandlerTests()
  {
    _unitOfWork.Matches.Returns(_matches);
    _sut = new GetMatchInjuriesHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenNoLineup_ReturnsNull()
  {
    // Arrange
    _matches.GetLineup(5).Returns((Lineup?)null);

    // Act
    var result = await _sut.Handle(new GetMatchInjuriesQuery(5), CancellationToken.None);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task Handle_WhenLineupExists_MapsInjuriesPerTeam()
  {
    // Arrange
    var home = new TeamLineup
    {
      LineupType = LineupType.Confirmed,
      Players = [],
      Injuries = [new InjuryEntry(FootballPosition.ST, "S1", InjuryStatus.Out)]
    };
    var away = new TeamLineup
    {
      LineupType = LineupType.Predicted,
      Players = [],
      Injuries = [new InjuryEntry(FootballPosition.GK, "S2", InjuryStatus.Questionable)]
    };
    var lineup = new Lineup
    {
      MatchId = 1,
      HomeTeamJson = JsonSerializer.Serialize(home, JsonOpts),
      AwayTeamJson = JsonSerializer.Serialize(away, JsonOpts)
    };
    _matches.GetLineup(1).Returns(lineup);

    // Act
    var result = await _sut.Handle(new GetMatchInjuriesQuery(1), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.Home.Injuries.Should().ContainSingle()
      .Which.Should().Match<InjuriedPlayer>(p =>
        p.Name == "S1" && p.Position == FootballPosition.ST.ToString() && p.InjuryStatus == InjuryStatus.Out.ToString());
    result.Away.Injuries.Should().ContainSingle()
      .Which.Should().Match<InjuriedPlayer>(p =>
        p.Name == "S2" && p.Position == FootballPosition.GK.ToString() && p.InjuryStatus == InjuryStatus.Questionable.ToString());
  }
}
