using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Betting.CancelBetSlip;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using BankrollEntry = NoMoreBets.Domain.Bankrolls.Bankroll;

namespace NoMoreBets.Application.Tests.Betting.CancelBetSlip;

public class CancelBetSlipHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBettingRepository _bettingRepository = Substitute.For<IBettingRepository>();
  private readonly IBankrollRepository _bankrollRepository = Substitute.For<IBankrollRepository>();
  private readonly CancelBetSlipHandler _sut;

  public CancelBetSlipHandlerTests()
  {
    _unitOfWork.Betting.Returns(_bettingRepository);
    _unitOfWork.Bankroll.Returns(_bankrollRepository);
    _sut = new CancelBetSlipHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenAllSelectionsPending_CancelsSlipAndRefundsStake()
  {
    // Arrange
    var slip = new BetSlip
    {
      Id = 42,
      StakeAmount = 25m,
      BetStatus = BetStatus.Pending,
      Selections =
      [
        new BetSelection { BetStatus = BetStatus.Pending },
        new BetSelection { BetStatus = BetStatus.Pending }
      ]
    };
    _bettingRepository.GetBetSlipWithSelectionsByIdAsync(42, Arg.Any<CancellationToken>())
      .Returns(slip);

    // Act
    var result = await _sut.Handle(new CancelBetSlipCommand(42), CancellationToken.None);

    // Assert
    result.Should().Be(Unit.Value);
    slip.BetStatus.Should().Be(BetStatus.Canceled);
    slip.Selections.Should().OnlyContain(s => s.BetStatus == BetStatus.Canceled);
    await _bankrollRepository.Received(1).AddAsync(
      Arg.Is<BankrollEntry>(entry =>
        entry.BetId == slip.Id
        && entry.Amount == slip.StakeAmount
        && entry.Direction == BankrollFlow.In),
      Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenAnySelectionNotPending_ThrowsAndDoesNotSave()
  {
    // Arrange
    var slip = new BetSlip
    {
      Id = 77,
      BetStatus = BetStatus.Pending,
      Selections =
      [
        new BetSelection { BetStatus = BetStatus.Pending },
        new BetSelection { BetStatus = BetStatus.Won }
      ]
    };
    _bettingRepository.GetBetSlipWithSelectionsByIdAsync(77, Arg.Any<CancellationToken>())
      .Returns(slip);

    // Act
    var action = () => _sut.Handle(new CancelBetSlipCommand(77), CancellationToken.None);

    // Assert
    await action.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("*cannot be canceled*");
    await _bankrollRepository.DidNotReceive().AddAsync(Arg.Any<BankrollEntry>(), Arg.Any<CancellationToken>());
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenSlipNotFound_ThrowsAndDoesNotSave()
  {
    // Arrange
    _bettingRepository.GetBetSlipWithSelectionsByIdAsync(999, Arg.Any<CancellationToken>())
      .Returns((BetSlip?)null);

    // Act
    var action = () => _sut.Handle(new CancelBetSlipCommand(999), CancellationToken.None);

    // Assert
    await action.Should().ThrowAsync<KeyNotFoundException>()
      .WithMessage("*999*");
    await _bankrollRepository.DidNotReceive().AddAsync(Arg.Any<BankrollEntry>(), Arg.Any<CancellationToken>());
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
