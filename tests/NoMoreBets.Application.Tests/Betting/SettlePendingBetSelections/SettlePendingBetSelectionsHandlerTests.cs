using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Betting.SettlePendingBetSelections;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Betting;
using BankrollEntry = NoMoreBets.Domain.Bankrolls.Bankroll;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Betting.SettlePendingBetSelections;

public class SettlePendingBetSelectionsHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _betting = Substitute.For<IBettingRepository>();
  private readonly IBankrollRepository _bankroll = Substitute.For<IBankrollRepository>();
  private readonly SettlePendingBetSelectionsHandler _sut;

  public SettlePendingBetSelectionsHandlerTests()
  {
    _unitOfWork.Betting.Returns(_betting);
    _unitOfWork.Bankroll.Returns(_bankroll);
    _sut = new SettlePendingBetSelectionsHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_DoesNotSave_WhenNoPendingSelectionsWithScores()
  {
    _betting.GetPendingSelectionsWithBothScoresAsync(Arg.Any<CancellationToken>())
      .Returns(Array.Empty<BetSelection>());

    var result = await _sut.Handle(new SettlePendingBetSelectionsCommand(), CancellationToken.None);

    result.Should().Be(Unit.Value);
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_SingleWon_SetsSlipWon_AndCreditsBankroll()
  {
    var match = new Match { Id = 1, HomeGoals = 2, AwayGoals = 0 };
    var slip = new BetSlip
    {
      Id = 10,
      PotentialPayout = 50m,
      BetStatus = BetStatus.Pending,
      Selections = new List<BetSelection>()
    };
    var selection = new BetSelection
    {
      Id = 1,
      BetSlipId = 10,
      MatchId = 1,
      BetEventOption = BettingEventOption.MatchResult_Home,
      BetStatus = BetStatus.Pending,
      Match = match,
      BetSlip = slip
    };
    slip.Selections.Add(selection);

    _betting.GetPendingSelectionsWithBothScoresAsync(Arg.Any<CancellationToken>())
      .Returns(new List<BetSelection> { selection });

    var result = await _sut.Handle(new SettlePendingBetSelectionsCommand(), CancellationToken.None);

    result.Should().Be(Unit.Value);
    selection.BetStatus.Should().Be(BetStatus.Won);
    slip.BetStatus.Should().Be(BetStatus.Won);
    await _bankroll.Received(1).AddAsync(Arg.Any<BankrollEntry>(), Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ParlayOneLegLost_MarksSlipLost_WithoutPayout()
  {
    var match1 = new Match { Id = 1, HomeGoals = 2, AwayGoals = 0 };
    var match2 = new Match { Id = 2, HomeGoals = null, AwayGoals = null };
    var slip = new BetSlip
    {
      Id = 20,
      PotentialPayout = 100m,
      BetStatus = BetStatus.Pending,
      Selections = new List<BetSelection>()
    };
    var lostLeg = new BetSelection
    {
      Id = 1,
      BetSlipId = 20,
      MatchId = 1,
      BetEventOption = BettingEventOption.MatchResult_Away,
      BetStatus = BetStatus.Pending,
      Match = match1,
      BetSlip = slip
    };
    var stillPending = new BetSelection
    {
      Id = 2,
      BetSlipId = 20,
      MatchId = 2,
      BetEventOption = BettingEventOption.MatchResult_Home,
      BetStatus = BetStatus.Pending,
      Match = match2,
      BetSlip = slip
    };
    slip.Selections.Add(lostLeg);
    slip.Selections.Add(stillPending);

    _betting.GetPendingSelectionsWithBothScoresAsync(Arg.Any<CancellationToken>())
      .Returns(new List<BetSelection> { lostLeg });

    var result = await _sut.Handle(new SettlePendingBetSelectionsCommand(), CancellationToken.None);

    result.Should().Be(Unit.Value);
    lostLeg.BetStatus.Should().Be(BetStatus.Lost);
    stillPending.BetStatus.Should().Be(BetStatus.Pending);
    slip.BetStatus.Should().Be(BetStatus.Lost);
    await _bankroll.DidNotReceive().AddAsync(Arg.Any<BankrollEntry>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ParlayBothLegsWin_MarksSlipWon_Once()
  {
    var m1 = new Match { Id = 1, HomeGoals = 1, AwayGoals = 0 };
    var m2 = new Match { Id = 2, HomeGoals = 0, AwayGoals = 1 };
    var slip = new BetSlip
    {
      Id = 30,
      PotentialPayout = 80m,
      BetStatus = BetStatus.Pending,
      Selections = new List<BetSelection>()
    };
    var s1 = new BetSelection
    {
      Id = 1,
      BetSlipId = 30,
      MatchId = 1,
      BetEventOption = BettingEventOption.MatchResult_Home,
      BetStatus = BetStatus.Pending,
      Match = m1,
      BetSlip = slip
    };
    var s2 = new BetSelection
    {
      Id = 2,
      BetSlipId = 30,
      MatchId = 2,
      BetEventOption = BettingEventOption.MatchResult_Away,
      BetStatus = BetStatus.Pending,
      Match = m2,
      BetSlip = slip
    };
    slip.Selections.Add(s1);
    slip.Selections.Add(s2);

    _betting.GetPendingSelectionsWithBothScoresAsync(Arg.Any<CancellationToken>())
      .Returns(new List<BetSelection> { s1, s2 });

    var result = await _sut.Handle(new SettlePendingBetSelectionsCommand(), CancellationToken.None);

    result.Should().Be(Unit.Value);
    s1.BetStatus.Should().Be(BetStatus.Won);
    s2.BetStatus.Should().Be(BetStatus.Won);
    slip.BetStatus.Should().Be(BetStatus.Won);
    await _bankroll.Received(1).AddAsync(
      Arg.Is<BankrollEntry>(b => b.Amount == 80m),
      Arg.Any<CancellationToken>());
  }
}
