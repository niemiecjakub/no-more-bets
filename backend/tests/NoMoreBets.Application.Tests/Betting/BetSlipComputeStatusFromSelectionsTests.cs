using FluentAssertions;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Tests.Betting;

public class BetSlipComputeStatusFromSelectionsTests
{
  [Fact]
  public void EmptySelections_IsPending()
  {
    var slip = new BetSlip { Selections = new List<BetSelection>() };
    slip.ComputeStatusFromSelections().Should().Be(BetStatus.Pending);
  }

  [Fact]
  public void AnyLost_IsLost()
  {
    var slip = new BetSlip
    {
      Selections =
      [
        new BetSelection { BetStatus = BetStatus.Won },
        new BetSelection { BetStatus = BetStatus.Lost }
      ]
    };
    slip.ComputeStatusFromSelections().Should().Be(BetStatus.Lost);
  }

  [Fact]
  public void AllWon_IsWon()
  {
    var slip = new BetSlip
    {
      Selections =
      [
        new BetSelection { BetStatus = BetStatus.Won },
        new BetSelection { BetStatus = BetStatus.Won }
      ]
    };
    slip.ComputeStatusFromSelections().Should().Be(BetStatus.Won);
  }

  [Fact]
  public void MixPending_IsPending()
  {
    var slip = new BetSlip
    {
      Selections =
      [
        new BetSelection { BetStatus = BetStatus.Won },
        new BetSelection { BetStatus = BetStatus.Pending }
      ]
    };
    slip.ComputeStatusFromSelections().Should().Be(BetStatus.Pending);
  }
}
