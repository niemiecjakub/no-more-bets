using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Features.Rotowire.GetRotowireLineups.Dtos;

/// <summary>
/// Shared DTO for position (acronym + full name). Accept an acronym and get the object.
/// </summary>
/// <param name="Acronym">Position code (e.g. GK, DL).</param>
/// <param name="FullName">Full display name (e.g. Goalkeeper).</param>
public record PlayerPositionDto(string Acronym, string FullName)
{
    /// <summary>Build from an acronym string; full name is resolved via the position map, or the acronym if unknown.</summary>
    public static PlayerPositionDto FromAcronym(string? acronym)
    {
        if (string.IsNullOrWhiteSpace(acronym))
            return new PlayerPositionDto("Unknown", "Unknown");
        var trimmed = acronym.Trim();
        if (!FootballPositions.TryParseFromAcronym(trimmed, out var position))
            return new PlayerPositionDto(trimmed, trimmed);
        return From(position);
    }

    /// <summary>Build from a domain <see cref="FootballPosition"/>.</summary>
    public static PlayerPositionDto From(FootballPosition position)
    {
        return new PlayerPositionDto(
            FootballPositions.GetAcronym(position),
            FootballPositions.GetFullName(position));
    }
}
