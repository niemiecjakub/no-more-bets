namespace NoMoreBets.Domain.Matches;


/// <summary>
/// Stored shape of FotmobDetailsJson: lineups and stats only (no team names, date, or score).
/// Deserialize from MatchDetails.FotmobDetailsJson when reading from DB.
/// </summary>
public record FotmobDetailsPayload(
  FotmobTeamLineup? HomeLineup,
  FotmobTeamLineup? AwayLineup,
  IReadOnlyList<FotmobStatGroup>? Statistics,
  IReadOnlyList<FotmobPlayerMatchStats>? Players);


/// <summary>Single player in a match lineup from FotMob match detail page.</summary>
public class FotmobLineupPlayer
{
  public required string Name { get; init; }
  public double? Rating { get; init; }
}

/// <summary>Team lineup (formation, rating, players) from FotMob match detail page.</summary>
public class FotmobTeamLineup
{
  public required string TeamName { get; init; }
  public string? Formation { get; init; }
  public double? TeamRating { get; init; }
  public required IReadOnlyList<FotmobLineupPlayer> Players { get; init; }
}

/// <summary>Single stat row (label + home/away values) within a stat group from FotMob Statistics tab.</summary>
public class FotmobStatRow
{
  public required string Label { get; init; }
  public string? HomeValue { get; init; }
  public string? AwayValue { get; init; }
}

/// <summary>Group of statistics (e.g. Possession, Shots) from FotMob Statistics tab.</summary>
public class FotmobStatGroup
{
  public required string Title { get; init; }
  public required IReadOnlyList<FotmobStatRow> Rows { get; init; }
}

/// <summary>Per-player match statistics from FotMob Statistics tab (individual stats table).</summary>
public class FotmobPlayerMatchStats
{
  public required string Player { get; init; }
  public required string Score { get; init; }
  public required string MinutesPlayed { get; init; }
  public required string Goals { get; init; }
  public required string Assists { get; init; }
  public required string Xg { get; init; }
  public required string Xa { get; init; }
  public required string XgPlusXa { get; init; }
  public required string DefensiveContributions { get; init; }
}
