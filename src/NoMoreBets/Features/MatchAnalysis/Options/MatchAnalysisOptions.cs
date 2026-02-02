namespace NoMoreBets.Features.MatchAnalysis.Options;

/// <summary>
/// Configuration options for the Match Analysis orchestrator.
/// </summary>
public class MatchAnalysisOptions
{
  /// <summary>League ID for fetching upcoming match previews (e.g. Premier League = 228).</summary>
  public int LeagueId { get; set; } = 228;

  /// <summary>Output directory for persisting match analysis results (JSON files).</summary>
  public string OutputDirectory { get; set; } = "Output";
}
