using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Common.Dto.Leagues;

/// <summary>
/// Represents a club entry in the FotMob league table (standings).
/// </summary>
public class TableEntry
{
    /// <summary>League position/rank.</summary>
    public int Position { get; init; }

    /// <summary>Full team name.</summary>
    public string TeamName { get; init; } = string.Empty;

    /// <summary>Short team name.</summary>
    public string TeamShortname { get; init; } = string.Empty;

    /// <summary>Team ID extracted from URL.</summary>
    public int TeamId { get; init; }

    /// <summary>Team logo image URL.</summary>
    public string TeamLogoUrl { get; init; } = string.Empty;

    /// <summary>Number of matches played.</summary>
    public int MatchesPlayed { get; init; }

    /// <summary>Number of wins.</summary>
    public int Wins { get; init; }

    /// <summary>Number of draws.</summary>
    public int Draws { get; init; }

    /// <summary>Number of losses.</summary>
    public int Losses { get; init; }

    /// <summary>Goals scored.</summary>
    public int GoalsFor { get; init; }

    /// <summary>Goals conceded.</summary>
    public int GoalsAgainst { get; init; }

    /// <summary>Goal difference as string (e.g. "+26", "-5").</summary>
    public string GoalDifference { get; init; } = string.Empty;

    /// <summary>Total points.</summary>
    public int Points { get; init; }

    /// <summary>Last 5 results (e.g. Win, Win, Draw, Draw, Loss).</summary>
    public IReadOnlyList<MatchResult> Form { get; init; } = Array.Empty<MatchResult>();

    /// <summary>Next opponent team ID.</summary>
    public int? NextOpponentId { get; init; }

    /// <summary>Next opponent team name.</summary>
    public string? NextOpponentName { get; init; }

    /// <summary>Next opponent logo URL.</summary>
    public string? NextOpponentLogoUrl { get; init; }
}
