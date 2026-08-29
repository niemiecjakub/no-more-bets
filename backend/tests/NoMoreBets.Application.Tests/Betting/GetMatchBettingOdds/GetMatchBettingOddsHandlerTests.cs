using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Betting.GetMatchBettingOdds;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;

namespace NoMoreBets.Application.Tests.Betting.GetMatchBettingOdds;

public class GetMatchBettingOddsHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();
  private readonly GetMatchBettingOddsHandler _sut;

  public GetMatchBettingOddsHandlerTests()
  {
    _unitOfWork.Betting.Returns(_betting);
    _sut = new GetMatchBettingOddsHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenNoSnapshots_ReturnsEmpty()
  {
    _betting.GetBettingOddsSnapshotsForMatchAsync(3, Arg.Any<CancellationToken>())
      .Returns([]);

    var result = await _sut.Handle(new GetMatchBettingOddsQuery(3), CancellationToken.None);

    result.Should().BeEmpty();
  }

  [Fact]
  public async Task Handle_SkipsRowsWithoutNameOrOdds_AndMapsValidRows()
  {
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

    var result = await _sut.Handle(new GetMatchBettingOddsQuery(7), CancellationToken.None);

    result.Should().ContainSingle();
    var market = result[0];
    market.EventTypeId.Should().Be(5);
    market.Options.Should().ContainSingle()
      .Which.Should().Be(new CurrentOddsOption("Away", 2.1));
  }

  [Fact]
  public async Task Handle_GroupsMultipleOutcomesUnderSameEventType()
  {
    var snapshot = new BettingOddsSnapshot
    {
      Rows =
      [
        new BettingOddsSnapshotRow
        {
          EventTypeId = 5,
          Odds = 1.5m,
          EventTypeEntity = new BettingEventTypeEntity { Name = "MatchResult" },
          EventOptionEntity = new BettingEventOptionEntity { Name = "MatchResult_Home" }
        },
        new BettingOddsSnapshotRow
        {
          EventTypeId = 5,
          Odds = 4m,
          EventTypeEntity = new BettingEventTypeEntity { Name = "MatchResult" },
          EventOptionEntity = new BettingEventOptionEntity { Name = "MatchResult_Draw" }
        },
        new BettingOddsSnapshotRow
        {
          EventTypeId = 4,
          Odds = 1.9m,
          EventTypeEntity = new BettingEventTypeEntity { Name = "BothTeamsToScore" },
          EventOptionEntity = new BettingEventOptionEntity { Name = "BothTeamsToScore_Yes" }
        }
      ]
    };
    _betting.GetBettingOddsSnapshotsForMatchAsync(99, Arg.Any<CancellationToken>())
      .Returns([snapshot]);

    var result = await _sut.Handle(new GetMatchBettingOddsQuery(99), CancellationToken.None);

    result.Should().HaveCount(2);
    result.Should().BeInAscendingOrder(m => m.EventTypeId);
    var matchResult = result.Should().ContainSingle(m => m.EventTypeId == 5).Subject;
    matchResult.Options.Should().HaveCount(2)
      .And.Contain(new CurrentOddsOption("MatchResult_Home", 1.5))
      .And.Contain(new CurrentOddsOption("MatchResult_Draw", 4));
  }

  [Fact]
  public async Task Handle_WhenExoticMarketsExcluded_OmitsHandicap()
  {
    var snapshot = new BettingOddsSnapshot
    {
      Rows =
      [
        new BettingOddsSnapshotRow
        {
          EventTypeId = 11,
          Odds = 2m,
          EventTypeEntity = new BettingEventTypeEntity { Name = "Handicap" },
          EventOptionEntity = new BettingEventOptionEntity { Name = "Handicap_Home_Plus_1" }
        },
        new BettingOddsSnapshotRow
        {
          EventTypeId = 5,
          Odds = 3m,
          EventTypeEntity = new BettingEventTypeEntity { Name = "MatchResult" },
          EventOptionEntity = new BettingEventOptionEntity { Name = "MatchResult_Draw" }
        }
      ]
    };
    _betting.GetBettingOddsSnapshotsForMatchAsync(8, Arg.Any<CancellationToken>())
      .Returns([snapshot]);

    var compact = await _sut.Handle(new GetMatchBettingOddsQuery(8), CancellationToken.None);
    var full = await _sut.Handle(new GetMatchBettingOddsQuery(8, true), CancellationToken.None);

    compact.Should().ContainSingle().Which.EventTypeId.Should().Be(5);
    full.Should().HaveCount(2);
    full.Select(m => m.EventTypeId).Should().BeEquivalentTo([5, 11]);
  }
}
