namespace NoMoreBets.Data.Models;

/// <summary>
/// Domain model representing a football match with structured team data.
/// This is the clean, object-oriented representation used throughout the application.
/// </summary>
public class Match
{
    public string Division { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public string? Referee { get; set; }
    public MatchResult? FullTimeResult { get; set; }
    public MatchResult? HalfTimeResult { get; set; }

    /// <summary>
    /// Dictionary containing team data for both home and away teams.
    /// Use TeamSide enum as the key to access team-specific information.
    /// </summary>
    public Dictionary<TeamSide, TeamMatchData> Teams { get; set; } = new();
}
