namespace NoMoreBets.Application.Leagues.UpdateTable;

/// <summary>Thrown when scraped league table data does not cover every club in the active season.</summary>
public sealed class IncompleteLeagueTableDataException : Exception
{
  public IncompleteLeagueTableDataException(
    int leagueId,
    IReadOnlyList<string> missingTableDataForClubs,
    IReadOnlyList<string> missingXgDataForClubs,
    IReadOnlyList<string> unmatchedTableTeams)
    : base(BuildMessage(leagueId, missingTableDataForClubs, missingXgDataForClubs, unmatchedTableTeams))
  {
    LeagueId = leagueId;
    MissingTableDataForClubs = missingTableDataForClubs;
    MissingXgDataForClubs = missingXgDataForClubs;
    UnmatchedTableTeams = unmatchedTableTeams;
  }

  public int LeagueId { get; }

  public IReadOnlyList<string> MissingTableDataForClubs { get; }

  public IReadOnlyList<string> MissingXgDataForClubs { get; }

  public IReadOnlyList<string> UnmatchedTableTeams { get; }

  private static string BuildMessage(
    int leagueId,
    IReadOnlyList<string> missingTableDataForClubs,
    IReadOnlyList<string> missingXgDataForClubs,
    IReadOnlyList<string> unmatchedTableTeams)
  {
    var parts = new List<string>
    {
      $"Cannot save league table snapshot for league {leagueId}: incomplete data."
    };

    if (missingTableDataForClubs.Count > 0)
    {
      parts.Add($"Missing table data for: {string.Join(", ", missingTableDataForClubs)}.");
    }

    if (missingXgDataForClubs.Count > 0)
    {
      parts.Add($"Missing xG data for: {string.Join(", ", missingXgDataForClubs)}.");
    }

    if (unmatchedTableTeams.Count > 0)
    {
      parts.Add($"Unmatched table teams: {string.Join(", ", unmatchedTableTeams)}.");
    }

    return string.Join(" ", parts);
  }
}
