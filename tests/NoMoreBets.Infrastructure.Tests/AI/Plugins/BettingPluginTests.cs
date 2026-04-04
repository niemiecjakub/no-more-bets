using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.AI.Plugins;
using NoMoreBets.Infrastructure.AI.Plugins.Models;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Infrastructure.Tests.AI.Plugins;

public class BettingPluginTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly BettingPlugin _sut;

  public BettingPluginTests()
  {
    _unitOfWork.Betting.Returns(_betting);
    _sut = new BettingPlugin(_unitOfWork, _mediator);
  }

  [Fact]
  public async Task GetAvailableMatchesAsync_MapsClubNamesAndIds()
  {
    // Arrange
    var when = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc);
    var matches = new List<Match>
    {
      new()
      {
        Id = 10,
        MatchDate = when,
        HomeClub = new ClubEntity { Name = "H" },
        AwayClub = new ClubEntity { Name = "A" }
      }
    };
    _betting.GetMatchesAvailableForBettingAsync(Arg.Any<CancellationToken>())
      .Returns(matches);

    // Act
    var result = await _sut.GetAvailableMatchesAsync(CancellationToken.None);

    // Assert
    result.Should().ContainSingle();
    result[0].Should().Be(new AvailableMatch(10, "H", "A", when));
  }

  [Fact]
  public async Task GetCurrentOddsAsync_WhenNoSnapshots_ReturnsEmpty()
  {
    // Arrange
    _betting.GetBettingOddsSnapshotsForMatchAsync(3, Arg.Any<CancellationToken>())
      .Returns([]);

    // Act
    var result = await _sut.GetCurrentOddsAsync(3, CancellationToken.None);

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public async Task GetCurrentOddsAsync_SkipsRowsWithoutNameOrOdds_AndMapsValidRows()
  {
    // Arrange
    var snapshot = new BettingOddsSnapshot
    {
      Rows =
      [
        new BettingOddsSnapshotRow
        {
          EventTypeId = 5,
          Odds = null,
          EventTypeEntity = new BettingEventTypeEntity { Name = "Match Result" },
          EventOptionEntity = new BettingEventOptionEntity { Name = "Home" }
        },
        new BettingOddsSnapshotRow
        {
          EventTypeId = 5,
          Odds = 2.1m,
          EventTypeEntity = new BettingEventTypeEntity { Name = "Match Result" },
          EventOptionEntity = new BettingEventOptionEntity { Name = "Away" }
        }
      ]
    };
    _betting.GetBettingOddsSnapshotsForMatchAsync(7, Arg.Any<CancellationToken>())
      .Returns([snapshot]);

    // Act
    var result = await _sut.GetCurrentOddsAsync(7, CancellationToken.None);

    // Assert
    result.Should().ContainSingle();
    var market = result[0];
    market.EventTypeId.Should().Be(5);
    market.Options.Should().ContainSingle()
      .Which.Should().Be(new CurrentOddsOption("Away", 2.1));
  }
}
