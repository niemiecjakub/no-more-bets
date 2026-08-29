using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Betting.GetMatchBettingOdds;
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
  public async Task GetCurrentOddsAsync_WhenCalled_DispatchesGetMatchBettingOddsQuery()
  {
    var expected = new CurrentOddsMarket(5, "MatchResult", [new CurrentOddsOption("MatchResult_Home", 1.5)]);
    _mediator.Send(Arg.Any<GetMatchBettingOddsQuery>(), Arg.Any<CancellationToken>())
      .Returns((IReadOnlyList<CurrentOddsMarket>)[expected]);

    var result = await _sut.GetCurrentOddsAsync(3, includeExoticMarkets: true, cancellationToken: CancellationToken.None);

    result.Should().ContainSingle().Which.Should().Be(expected);
    await _mediator.Received(1).Send(
      Arg.Is<GetMatchBettingOddsQuery>(q => q.MatchId == 3 && q.IncludeExoticMarkets),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetCurrentOddsForMarketAsync_WhenCalled_DispatchesFullOddsQueryAndReturnsRequestedMarket()
  {
    var matchResult = new CurrentOddsMarket(5, "MatchResult", [new CurrentOddsOption("MatchResult_Home", 1.5)]);
    var btts = new CurrentOddsMarket(4, "BothTeamsToScore", [new CurrentOddsOption("BothTeamsToScore_Yes", 1.9)]);
    _mediator.Send(Arg.Any<GetMatchBettingOddsQuery>(), Arg.Any<CancellationToken>())
      .Returns((IReadOnlyList<CurrentOddsMarket>)[matchResult, btts]);

    var result = await _sut.GetCurrentOddsForMarketAsync(7, BettingEventType.BothTeamsToScore);

    result.Should().ContainSingle().Which.Should().Be(btts);
    await _mediator.Received(1).Send(
      Arg.Is<GetMatchBettingOddsQuery>(q => q.MatchId == 7 && q.IncludeExoticMarkets),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task GetCurrentOddsForMarketAsync_WhenMarketMissing_ReturnsEmpty()
  {
    var matchResult = new CurrentOddsMarket(5, "MatchResult", [new CurrentOddsOption("MatchResult_Home", 1.5)]);
    _mediator.Send(Arg.Any<GetMatchBettingOddsQuery>(), Arg.Any<CancellationToken>())
      .Returns((IReadOnlyList<CurrentOddsMarket>)[matchResult]);

    var result = await _sut.GetCurrentOddsForMarketAsync(7, BettingEventType.Handicap);

    result.Should().BeEmpty();
  }

  [Fact]
  public async Task PlaceBetSlip_WhenStakeIsZero_ThrowsAndDoesNotAddBetSlip()
  {
    const string json =
      """{"betSelections":[{"matchId":1,"eventType":"bothTeamsToScore","eventOption":"bothTeamsToScore_Yes"}]}""";

    var act = async () => await _sut.PlaceBetSlip(0m, json, "test rationale", 0.5m, CancellationToken.None);

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

    var act = async () => await _sut.PlaceBetSlip(50m, json, "test rationale", 0.5m, CancellationToken.None);

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

    await _sut.PlaceBetSlip(25m, json, "BTTS edge from research", 0.55m, CancellationToken.None);

    await _betting.Received(1).AddBetSlipAsync(
      Arg.Is<BetSlip>(s =>
        s.StakeAmount == 25m
        && s.TotalOdds == 2.0m
        && s.PotentialPayout == 50m
        && s.Rationale == "BTTS edge from research"
        && s.EstimatedWinProbability == 0.55m
        && s.Bankrolls.Count == 1
        && s.Bankrolls.Single().Amount == 25m
        && s.Bankrolls.Single().Direction == BankrollFlow.Out
        && s.Bankrolls.Single().Name == "Bet stake"),
      Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
