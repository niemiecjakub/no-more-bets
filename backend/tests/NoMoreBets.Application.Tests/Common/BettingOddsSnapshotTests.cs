using FluentAssertions;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Tests.Common;

public class BettingOddsSnapshotTests
{
  [Fact]
  public void EnsureCompleteBettingEventOptionsCoverage_throws_when_snapshot_is_missing_options()
  {
    var snapshot = new BettingOddsSnapshot
    {
      Rows =
      [
        new BettingOddsSnapshotRow { EventOption = BettingEventOption.MatchResult_Home },
        new BettingOddsSnapshotRow { EventOption = BettingEventOption.MatchResult_Away }
      ]
    };

    var act = () => snapshot.EnsureCompleteBettingEventOptionsCoverage();

    act.Should().Throw<InvalidOperationException>();
  }

  [Fact]
  public void EnsureCompleteBettingEventOptionsCoverage_does_not_throw_when_snapshot_has_all_required_core_options()
  {
    var snapshot = new BettingOddsSnapshot
    {
      Rows =
      [
        new BettingOddsSnapshotRow { EventOption = BettingEventOption.MatchResult_Home },
        new BettingOddsSnapshotRow { EventOption = BettingEventOption.MatchResult_Away },
        new BettingOddsSnapshotRow { EventOption = BettingEventOption.MatchResult_Draw },
        new BettingOddsSnapshotRow { EventOption = BettingEventOption.BothTeamsToScore_Yes },
        new BettingOddsSnapshotRow { EventOption = BettingEventOption.BothTeamsToScore_No }
      ]
    };

    var act = () => snapshot.EnsureCompleteBettingEventOptionsCoverage();

    act.Should().NotThrow();
  }

  [Fact]
  public void EnsureCompleteBettingEventOptionsCoverage_does_not_throw_when_snapshot_has_required_and_extra_options()
  {
    var snapshot = new BettingOddsSnapshot
    {
      Rows = Enum.GetValues<BettingEventOption>()
        .Select(option => new BettingOddsSnapshotRow { EventOption = option })
        .ToList()
    };

    var act = () => snapshot.EnsureCompleteBettingEventOptionsCoverage();

    act.Should().NotThrow();
  }
}
