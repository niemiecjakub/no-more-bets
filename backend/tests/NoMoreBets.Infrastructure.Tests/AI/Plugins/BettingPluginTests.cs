using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Betting.GetMatchesAvailableForBetting;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;
using NoMoreBets.Infrastructure.AI.Tools.Implementations.Models;
using NoMoreBets.Infrastructure.AI.Common;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;

namespace NoMoreBets.Infrastructure.Tests.AI.Plugins;

public class BettingToolTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();
  private readonly IBankrollRepository _bankroll = Substitute.For<IBankrollRepository>();
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly AgentSessionContext _agentSessionContext = new();
  private readonly BettingTool _sut;

  public BettingToolTests()
  {
    _unitOfWork.Betting.Returns(_betting);
    _unitOfWork.Bankroll.Returns(_bankroll);
    _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    _sut = new BettingTool(_unitOfWork, _mediator, _agentSessionContext);
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
    _mediator
      .Send(Arg.Any<GetMatchesAvailableForBettingQuery>(), Arg.Any<CancellationToken>())
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
    var result = await _sut.GetCurrentOddsAsync(3, cancellationToken: CancellationToken.None);

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
    var result = await _sut.GetCurrentOddsAsync(7, cancellationToken: CancellationToken.None);

    // Assert
    result.Should().ContainSingle();
    var market = result[0];
    market.EventTypeId.Should().Be(5);
    market.Options.Should().ContainSingle()
      .Which.Should().Be(new CurrentOddsOption("Away", 2.1));
  }

  [Fact]
  public async Task GetCurrentOddsAsync_GroupsMultipleOutcomesUnderSameEventType()
  {
    // Arrange
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

    // Act
    var result = await _sut.GetCurrentOddsAsync(99, cancellationToken: CancellationToken.None);

    // Assert
    result.Should().HaveCount(2);
    result.Should().BeInAscendingOrder(m => m.EventTypeId);
    var matchResult = result.Should().ContainSingle(m => m.EventTypeId == 5).Subject;
    matchResult.Options.Should().HaveCount(2)
      .And.Contain(new CurrentOddsOption("MatchResult_Home", 1.5))
      .And.Contain(new CurrentOddsOption("MatchResult_Draw", 4));
  }

  [Fact]
  public async Task GetCurrentOddsAsync_WhenExoticMarketsExcluded_OmitsHandicap()
  {
    // Arrange
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

    // Act
    var compact = await _sut.GetCurrentOddsAsync(8, includeExoticMarkets: false, cancellationToken: CancellationToken.None);
    var full = await _sut.GetCurrentOddsAsync(8, includeExoticMarkets: true, cancellationToken: CancellationToken.None);

    // Assert
    compact.Should().ContainSingle().Which.EventTypeId.Should().Be(5);
    full.Should().HaveCount(2);
    full.Select(m => m.EventTypeId).Should().BeEquivalentTo([5, 11]);
  }

  [Fact]
  public async Task PlaceBetSlip_WhenStakeIsZero_ThrowsAndDoesNotAddBetSlip()
  {
    const string json =
      """{"betSelections":[{"matchId":1,"eventType":"bothTeamsToScore","eventOption":"bothTeamsToScore_Yes"}]}""";

    var act = async () => await _sut.PlaceBetSlip(0m, json, CancellationToken.None);

    await act.Should().ThrowAsync<ArgumentException>()
      .WithMessage("*stakeAmount must be greater than zero*")
      .Where(e => e.ParamName == "stakeAmount");
    await _betting.DidNotReceive().AddBetSlipAsync(Arg.Any<BetSlip>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task PlaceBetSlip_WhenStakeExceedsBalance_ThrowsAndDoesNotAddBetSlip()
  {
    const string json =
      """{"betSelections":[{"matchId":1,"eventType":"bothTeamsToScore","eventOption":"bothTeamsToScore_Yes"}]}""";
    _bankroll.GetCurrentBalanceAsync(Arg.Any<CancellationToken>()).Returns(40m);

    var act = async () => await _sut.PlaceBetSlip(50m, json, CancellationToken.None);

    await act.Should().ThrowAsync<ArgumentException>()
      .WithMessage("*cannot exceed the current bankroll balance*");
    await _betting.DidNotReceive().AddBetSlipAsync(Arg.Any<BetSlip>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task PlaceBetSlip_WhenValid_AddsSlipWithBankrollOutAndSaves()
  {
    const string json =
      """{"betSelections":[{"matchId":1,"eventType":"bothTeamsToScore","eventOption":"bothTeamsToScore_Yes"}]}""";
    _bankroll.GetCurrentBalanceAsync(Arg.Any<CancellationToken>()).Returns(100m);
    _betting.GetCurrentOddsForSelectionAsync(1, BettingEventType.BothTeamsToScore, BettingEventOption.BothTeamsToScore_Yes, Arg.Any<CancellationToken>())
      .Returns(2.0m);

    await _sut.PlaceBetSlip(25m, json, CancellationToken.None);

    await _betting.Received(1).AddBetSlipAsync(
      Arg.Is<BetSlip>(s =>
        s.StakeAmount == 25m
        && s.TotalOdds == 2.0m
        && s.PotentialPayout == 50m
        && s.Bankrolls.Count == 1
        && s.Bankrolls.Single().Amount == 25m
        && s.Bankrolls.Single().Direction == BankrollFlow.Out
        && s.Bankrolls.Single().Name == "Bet stake"),
      Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
