namespace NoMoreBets.Features.MatchAnalysis.Options;

/// <summary>
/// Configuration options for the Match Analysis orchestrator.
/// </summary>
public class MatchAnalysisOptions
{
  public const string SectionName = "MatchAnalysis";

  /// <summary>Output directory for persisting match analysis results (JSON files).</summary>
  public string OutputDirectory { get; set; } = "Output";
}
