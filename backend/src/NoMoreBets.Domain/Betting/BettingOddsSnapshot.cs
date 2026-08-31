using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Domain.Betting;

public class BettingOddsSnapshot
{
  private static readonly HashSet<BettingEventOption> RequiredOptions =
  [
    BettingEventOption.MatchResult_Home,
    BettingEventOption.MatchResult_Away,
    BettingEventOption.MatchResult_Draw,
    BettingEventOption.BothTeamsToScore_Yes,
    BettingEventOption.BothTeamsToScore_No
  ];

  public long Id { get; set; }
  public int MatchId { get; set; }
  public DateTime SnapshotTime { get; set; }

  public Match Match { get; set; } = null!;
  public ICollection<BettingOddsSnapshotRow> Rows { get; set; } = new List<BettingOddsSnapshotRow>();

  public void EnsureCompleteBettingEventOptionsCoverage()
  {
    var actualOptions = Rows
      .Where(row => row.EventOption.HasValue)
      .Select(row => row.EventOption!.Value)
      .ToHashSet();

    var missing = RequiredOptions
      .Where(option => !actualOptions.Contains(option))
      .OrderBy(option => (int)option)
      .ToList();

    if (missing.Count > 0)
    {
      var missingText = string.Join(", ", missing.Select(option => $"{option} ({(int)option})"));
      throw new InvalidOperationException(
        $"Betting odds snapshot for match {MatchId} must include all required core options. Missing: {missingText}.");
    }
  }
}
