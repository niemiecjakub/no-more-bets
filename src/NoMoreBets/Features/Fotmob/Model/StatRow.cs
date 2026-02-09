namespace NoMoreBets.Features.Fotmob.Model;

/// <summary>Single stat row (label + home/away values) within a stat group from FotMob Statistics tab.</summary>
public class StatRow
{
    public required string Label { get; init; }
    public string? HomeValue { get; init; }
    public string? AwayValue { get; init; }
}
