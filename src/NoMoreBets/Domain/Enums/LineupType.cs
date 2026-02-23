namespace NoMoreBets.Domain.Enums;

/// <summary>
/// Type of lineup (e.g. predicted vs confirmed).
/// Use <see cref="LineupTypes.GetDisplayName"/> for display name.
/// </summary>
public enum LineupType
{
  Unknown = 1,
  Predicted = 2,
  Confirmed = 3
}

/// <summary>
/// Immutable metadata for a lineup type.
/// </summary>
internal sealed class LineupTypeInfo
{
  public string StatusText { get; }
  public string DisplayName { get; }

  public LineupTypeInfo(string statusText, string displayName)
  {
    StatusText = statusText;
    DisplayName = displayName;
  }
}

public static class LineupTypes
{
  private static readonly IReadOnlyDictionary<LineupType, LineupTypeInfo> Types =
      new Dictionary<LineupType, LineupTypeInfo>
      {
            { LineupType.Unknown, new("Unknown", "Unknown Lineup") },
            { LineupType.Predicted, new("Predicted", "Predicted Lineup") },
            { LineupType.Confirmed, new("Confirmed", "Confirmed Lineup") }
      };

  /// <summary>
  /// Fragments to look for in status text (order matters: more specific first).
  /// </summary>
  private static readonly (string Fragment, LineupType Type)[] StatusTextFragments =
  {
        ("Confirmed Lineup", LineupType.Confirmed),
        ("Predicted Lineup", LineupType.Predicted),
        ("Unknown Lineup", LineupType.Unknown)
    };

  /// <summary>Returns the display name for API or UI (e.g. "Predicted Lineup", "Confirmed Lineup").</summary>
  public static string GetDisplayName(LineupType type)
  {
    return Types.TryGetValue(type, out var info)
        ? info.DisplayName
        : type.ToString();
  }

  /// <summary>
  /// Parses status text (e.g. from HTML) into a <see cref="LineupType"/>.
  /// Uses substring match: "Confirmed Lineup", "Predicted Lineup".
  /// </summary>
  public static bool TryParseFromStatusText(string? statusText, out LineupType result)
  {
    if (string.IsNullOrWhiteSpace(statusText))
    {
      result = LineupType.Unknown;
      return false;
    }

    var text = statusText.Trim();
    foreach (var (fragment, type) in StatusTextFragments)
    {
      if (text.Contains(fragment, StringComparison.Ordinal))
      {
        result = type;
        return true;
      }
    }

    result = LineupType.Unknown;
    return false;
  }
}
