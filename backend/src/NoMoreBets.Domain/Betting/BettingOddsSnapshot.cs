using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Domain.Betting;

public class BettingOddsSnapshot
{
  private static readonly HashSet<int> RequiredOptionIds =
  [
    (int)BettingEventOption.MatchResult_Home,
    (int)BettingEventOption.MatchResult_Away,
    (int)BettingEventOption.MatchResult_Draw,
    (int)BettingEventOption.BothTeamsToScore_Yes,
    (int)BettingEventOption.BothTeamsToScore_No
  ];

  public long Id { get; set; }
  public int MatchId { get; set; }
  public DateTime SnapshotTime { get; set; }

  public Match Match { get; set; } = null!;
  public ICollection<BettingOddsSnapshotRow> Rows { get; set; } = new List<BettingOddsSnapshotRow>();

  public void EnsureCompleteBettingEventOptionsCoverage()
  {
    var actualOptionIds = Rows
      .Where(row => row.EventOptionId.HasValue)
      .Select(row => row.EventOptionId!.Value)
      .ToHashSet();

    if (!RequiredOptionIds.IsSubsetOf(actualOptionIds))
    {
      var missingCount = RequiredOptionIds.Count - RequiredOptionIds.Count(actualOptionIds.Contains);
      throw new InvalidOperationException(
        $"Betting odds snapshot must include all required core options. Missing count: {missingCount}.");
    }
  }
}
