namespace NoMoreBets.Data.Models;

/// <summary>
/// Model representing a football match with structured team data.
/// </summary>
public class Match
{
    public string Division { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public string? Referee { get; set; }
    public MatchResult? FullTimeResult { get; set; }
    public MatchResult? HalfTimeResult { get; set; }
    public Dictionary<TeamSide, TeamMatchData> Teams { get; set; } = new();
}
 