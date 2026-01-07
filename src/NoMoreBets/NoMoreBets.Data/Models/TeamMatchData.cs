namespace NoMoreBets.Data.Models;

public class TeamMatchData
{
    public string TeamName { get; set; } = string.Empty;
    
    // Goals
    public int? FullTimeGoals { get; set; }
    public int? HalfTimeGoals { get; set; }
    
    // Match statistics
    public int? Shots { get; set; }
    public int? ShotsOnTarget { get; set; }
    public int? Corners { get; set; }
    public int? Fouls { get; set; }
    public int? Offsides { get; set; }
    public int? YellowCards { get; set; }
    public int? RedCards { get; set; }
}

