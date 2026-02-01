namespace NoMoreBets.Features.SoccerData.Model;

/// <summary>Season information.</summary>
public record Season
{
    public bool IsActive { get; init; }
    public string Year { get; init; } = string.Empty;
}
