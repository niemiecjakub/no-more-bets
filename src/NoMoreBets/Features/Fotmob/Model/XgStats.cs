namespace NoMoreBets.Features.Fotmob.Model;

/// <summary>
/// Represents xG statistics for a team in the FotMob league xG table.
/// </summary>
public class XgStats
{
    /// <summary>League position/rank.</summary>
    public int Position { get; init; }

    /// <summary>Change in position (positive for up, negative for down, null if no change).</summary>
    public int? PositionChange { get; init; }

    /// <summary>Team ID extracted from URL.</summary>
    public int TeamId { get; init; }

    /// <summary>Full team name.</summary>
    public string TeamName { get; init; } = string.Empty;

    /// <summary>Short team name.</summary>
    public string TeamShortname { get; init; } = string.Empty;

    /// <summary>Team logo image URL.</summary>
    public string TeamLogoUrl { get; init; } = string.Empty;

    /// <summary>Expected goals (main value).</summary>
    public double Xg { get; init; }

    /// <summary>Difference between expected and actual goals (e.g. "+0.7", "-2.5").</summary>
    public string? XgDiff { get; init; }

    /// <summary>Expected goals against (main value).</summary>
    public double Xga { get; init; }

    /// <summary>Difference between expected and actual goals against.</summary>
    public string? XgaDiff { get; init; }

    /// <summary>Expected points (main value).</summary>
    public double Xpts { get; init; }

    /// <summary>Difference between expected and actual points.</summary>
    public string? XptsDiff { get; init; }
}
