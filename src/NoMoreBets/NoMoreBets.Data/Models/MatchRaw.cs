using CsvHelper.Configuration.Attributes;

namespace NoMoreBets.Data.Models;

/// <summary>
/// Raw match data model directly mapped from CSV file.
/// This class contains all CSV column mappings and is used only for data import.
/// </summary>
public class MatchRaw
{
    // Core match information
    [Name("Div")]
    public string Division { get; set; } = string.Empty;

    [Name("Date")]
    public string DateString { get; set; } = string.Empty;

    [Name("Time")]
    public string Time { get; set; } = string.Empty;

    [Name("HomeTeam")]
    public string HomeTeam { get; set; } = string.Empty;

    [Name("AwayTeam")]
    public string AwayTeam { get; set; } = string.Empty;

    [Name("Referee")]
    public string? Referee { get; set; }

    // Results
    [Name("FTR")]
    public string? FullTimeResultString { get; set; }

    [Name("HTR")]
    public string? HalfTimeResultString { get; set; }

    // Home team data
    [Name("FTHG")]
    public int? FullTimeHomeGoals { get; set; }

    [Name("HTHG")]
    public int? HalfTimeHomeGoals { get; set; }

    [Name("HS")]
    public int? HomeShots { get; set; }

    [Name("HST")]
    public int? HomeShotsOnTarget { get; set; }

    [Name("HC")]
    public int? HomeCorners { get; set; }

    [Name("HF")]
    public int? HomeFouls { get; set; }

    [Name("HO")]
    public int? HomeOffsides { get; set; }

    [Name("HY")]
    public int? HomeYellowCards { get; set; }

    [Name("HR")]
    public int? HomeRedCards { get; set; }

    // Away team data
    [Name("FTAG")]
    public int? FullTimeAwayGoals { get; set; }

    [Name("HTAG")]
    public int? HalfTimeAwayGoals { get; set; }

    [Name("AS")]
    public int? AwayShots { get; set; }

    [Name("AST")]
    public int? AwayShotsOnTarget { get; set; }

    [Name("AC")]
    public int? AwayCorners { get; set; }

    [Name("AF")]
    public int? AwayFouls { get; set; }

    [Name("AO")]
    public int? AwayOffsides { get; set; }

    [Name("AY")]
    public int? AwayYellowCards { get; set; }

    [Name("AR")]
    public int? AwayRedCards { get; set; }
}

