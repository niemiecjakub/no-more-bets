using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchLineups;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using LineupPlayer = NoMoreBets.Application.Matches.GetMatchLineups.Player;

namespace NoMoreBets.Application.Tests.Matches.GetMatchLineups;

public class GetMatchLineupsHandlerTests
{
  private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly GetMatchLineupsHandler _sut;

  public GetMatchLineupsHandlerTests()
  {
    _unitOfWork.Matches.Returns(_matches);
    _sut = new GetMatchLineupsHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenNoLineup_ReturnsNull()
  {
    // Arrange
    _matches.GetLineup(8).Returns((Lineup?)null);

    // Act
    var result = await _sut.Handle(new GetMatchLineupsQuery(8), CancellationToken.None);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  public async Task Handle_WhenLineupExists_MapsLineupTypesAndPlayers()
  {
    // Arrange
    var home = new TeamLineup
    {
      LineupType = LineupType.Confirmed,
      Players = [new PlayerInLineup(FootballPosition.MC, "Mid1")],
      Injuries = []
    };
    var away = new TeamLineup
    {
      LineupType = LineupType.Predicted,
      Players = [new PlayerInLineup(FootballPosition.ST, "Fwd1")],
      Injuries = []
    };
    var lineup = new Lineup
    {
      MatchId = 2,
      HomeTeamJson = JsonSerializer.Serialize(home, JsonOpts),
      AwayTeamJson = JsonSerializer.Serialize(away, JsonOpts)
    };
    _matches.GetLineup(2).Returns(lineup);

    // Act
    var result = await _sut.Handle(new GetMatchLineupsQuery(2), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.Home.LineupType.Should().Be(LineupType.Confirmed.ToString());
    result.Away.LineupType.Should().Be(LineupType.Predicted.ToString());
    result.Home.Players.Should().ContainSingle()
      .Which.Should().Match<LineupPlayer>(p => p.Name == "Mid1" && p.Position == FootballPosition.MC.ToString());
    result.Away.Players.Should().ContainSingle()
      .Which.Should().Match<LineupPlayer>(p => p.Name == "Fwd1" && p.Position == FootballPosition.ST.ToString());
  }
}
