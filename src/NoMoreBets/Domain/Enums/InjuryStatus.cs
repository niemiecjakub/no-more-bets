namespace NoMoreBets.Domain.Enums;

/// <summary>
/// Injury/availability status (e.g. OUT, QUES, SUS).
/// Use <see cref="InjuryStatuses.GetFullName"/> for display name.
/// </summary>
public enum InjuryStatus
{
    Unknown = 0,
    Out,
    Questionable,
    Suspended,
    Doubtful
}

/// <summary>
/// Immutable metadata for an injury status.
/// </summary>
internal sealed class InjuryStatusInfo
{
    public string Code { get; }
    public string FullName { get; }
    public IReadOnlyCollection<string> Aliases { get; }

    public InjuryStatusInfo(string code, string fullName, params string[] aliases)
    {
        Code = code;
        FullName = fullName;
        Aliases = aliases ?? Array.Empty<string>();
    }
}

public static class InjuryStatuses
{
    private static readonly IReadOnlyDictionary<InjuryStatus, InjuryStatusInfo> Statuses =
        new Dictionary<InjuryStatus, InjuryStatusInfo>
        {
            { InjuryStatus.Unknown, new("?", "Unknown") },
            { InjuryStatus.Out, new("OUT", "Out") },
            { InjuryStatus.Questionable, new("QUES", "Questionable", "Questionable") }
        };

    /// <summary>
    /// Lookup table for parsing codes and aliases.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, InjuryStatus> CodeLookup =
        Statuses
            .SelectMany(kvp =>
                new[] { kvp.Value.Code }
                    .Concat(kvp.Value.Aliases)
                    .Select(c => new { Code = c, Status = kvp.Key }))
            .ToDictionary(
                x => x.Code,
                x => x.Status,
                StringComparer.OrdinalIgnoreCase
            );

    /// <summary>Returns the full display name for the status.</summary>
    public static string GetFullName(InjuryStatus status)
    {
        return Statuses.TryGetValue(status, out var info)
            ? info.FullName
            : status.ToString();
    }

    /// <summary>Returns the code string (e.g. "OUT", "QUES", "SUS").</summary>
    public static string GetCode(InjuryStatus status)
    {
        return Statuses.TryGetValue(status, out var info)
            ? info.Code
            : status.ToString();
    }

    /// <summary>
    /// Parses a code string (e.g. from HTML) into an <see cref="InjuryStatus"/>.
    /// Supports aliases such as "Questionable", "Suspended".
    /// </summary>
    public static bool TryParseFromCode(string? code, out InjuryStatus result)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            result = InjuryStatus.Unknown;
            return false;
        }

        return CodeLookup.TryGetValue(code.Trim(), out result);
    }
}
