namespace NoMoreBets.Configuration;

/// <summary>TEMP: Options for one-off maintenance HTTP actions. Remove when backfill endpoint is deleted.</summary>
public sealed class MaintenanceOptions
{
  public const string SectionName = "Maintenance";

  /// <summary>
  /// When non-empty, <c>POST .../backfill-fotmob-recent-match-details</c> in non-Development requires header
  /// <c>X-Backfill-Secret</c> with this exact value.
  /// </summary>
  public string? BackfillSecret { get; set; }
}
