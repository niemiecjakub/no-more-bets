using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Common;

/// <summary>
/// Shared DTO for position (acronym + full name). Accept an acronym and get the object.
/// </summary>
/// <param name="Acronym">Position code (e.g. GK, DL).</param>
/// <param name="FullName">Full display name (e.g. Goalkeeper).</param>
public record PositionDto(string Acronym, string FullName)
{
    /// <summary>Build from an acronym string; full name is resolved via the position map, or the acronym if unknown.</summary>
    public static PositionDto FromAcronym(string? acronym)
    {
        if (string.IsNullOrWhiteSpace(acronym))
            return new PositionDto("Unknown", "Unknown");
        var trimmed = acronym.Trim();
        if (!FootballPositions.TryParseFromAcronym(trimmed, out var position))
            return new PositionDto(trimmed, trimmed);
        return From(position);
    }

    /// <summary>Build from a domain <see cref="FootballPosition"/>.</summary>
    public static PositionDto From(FootballPosition position)
    {
        return new PositionDto(
            FootballPositions.GetAcronym(position),
            FootballPositions.GetFullName(position));
    }
}
