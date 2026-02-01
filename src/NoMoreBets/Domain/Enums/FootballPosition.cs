namespace NoMoreBets.Domain.Enums;

/// <summary>
/// Football position acronyms (e.g. GK, DL, ST).
/// Use <see cref="FootballPositions.GetFullName"/> for display name.
/// </summary>
public enum FootballPosition
{
    Unknown = 0,
    GK,
    DL,
    DR,
    DC,
    DMC,
    DM,
    ML,
    MR,
    MC,
    AMC,
    AML,
    AMR,
    LW,
    RW,
    FW,
    ST,
    M,
    D,
    F,
    G,
    DF,
    MD,
    FM
}


/// <summary>
/// Immutable metadata for a football position.
/// </summary>
internal sealed class FootballPositionInfo
{
    public string Acronym { get; }
    public string FullName { get; }
    public IReadOnlyCollection<string> Aliases { get; }

    public FootballPositionInfo(string acronym, string fullName, params string[] aliases)
    {
        Acronym = acronym;
        FullName = fullName;
        Aliases = aliases ?? Array.Empty<string>();
    }
}

public static class FootballPositions
{
    private static readonly IReadOnlyDictionary<FootballPosition, FootballPositionInfo> Positions =
        new Dictionary<FootballPosition, FootballPositionInfo>
        {
            { FootballPosition.Unknown, new("?", "Unknown") },

            { FootballPosition.GK,  new("GK",  "Goalkeeper", "G") },

            { FootballPosition.DL,  new("DL",  "Left Back / Left Defender") },
            { FootballPosition.DR,  new("DR",  "Right Back / Right Defender") },
            { FootballPosition.DC,  new("DC",  "Center Back / Central Defender") },

            { FootballPosition.DMC, new("DMC", "Defensive Midfielder / Central Defensive Midfielder") },
            { FootballPosition.DM,  new("DM",  "Defensive Midfielder") },

            { FootballPosition.ML,  new("ML",  "Left Midfielder / Left Wing Midfielder") },
            { FootballPosition.MR,  new("MR",  "Right Midfielder / Right Wing Midfielder") },
            { FootballPosition.MC,  new("MC",  "Central Midfielder") },

            { FootballPosition.AMC, new("AMC", "Attacking Midfielder / Central Attacking Midfielder") },
            { FootballPosition.AML, new("AML", "Attacking Midfielder / Left Attacking Midfielder") },
            { FootballPosition.AMR, new("AMR", "Attacking Midfielder / Right Attacking Midfielder") },

            // Rotowire-compatible aliases
            { FootballPosition.LW,  new("LW",  "Left Winger / Left Forward", "FWL") },
            { FootballPosition.RW,  new("RW",  "Right Winger / Right Forward", "FWR") },

            { FootballPosition.FW,  new("FW",  "Forward / Striker") },
            { FootballPosition.ST,  new("ST",  "Striker / Center Forward") },

            { FootballPosition.M,   new("M",   "Midfielder") },
            { FootballPosition.D,   new("D",   "Defender", "DF") },
            { FootballPosition.F,   new("F",   "Forward") },
            { FootballPosition.MD, new("M/D", "Midfielder / Defender") },

            { FootballPosition.FM,  new("F/M", "Forward / Midfielder") }
        };

    /// <summary>
    /// Lookup table for parsing acronyms and aliases.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, FootballPosition> AcronymLookup =
        Positions
            .SelectMany(kvp =>
                new[] { kvp.Value.Acronym }
                    .Concat(kvp.Value.Aliases)
                    .Select(a => new { Acronym = a, Position = kvp.Key }))
            .ToDictionary(
                x => x.Acronym,
                x => x.Position,
                StringComparer.OrdinalIgnoreCase
            );

    /// <summary>Returns the full display name for the position.</summary>
    public static string GetFullName(FootballPosition position)
    {
        return Positions.TryGetValue(position, out var info)
            ? info.FullName
            : position.ToString();
    }

    /// <summary>Returns the acronym string (e.g. "GK", "F/M").</summary>
    public static string GetAcronym(FootballPosition position)
    {
        return Positions.TryGetValue(position, out var info)
            ? info.Acronym
            : position.ToString();
    }

    /// <summary>
    /// Parses an acronym string (e.g. from HTML) into a <see cref="FootballPosition"/>.
    /// Supports aliases such as "F/M", "FWL", "FWR", "DF".
    /// </summary>
    public static bool TryParseFromAcronym(string? acronym, out FootballPosition result)
    {
        if (string.IsNullOrWhiteSpace(acronym))
        {
            result = FootballPosition.Unknown;
            return false;
        }

        return AcronymLookup.TryGetValue(acronym.Trim(), out result);
    }
}
