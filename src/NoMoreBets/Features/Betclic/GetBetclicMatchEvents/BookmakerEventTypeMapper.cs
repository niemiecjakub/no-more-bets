using System.ComponentModel.DataAnnotations;
using System.Reflection;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Features.Betclic.GetBetclicMatchEvents;

/// <summary>
/// Maps a bookmaker event title (e.g. from Betclic) to <see cref="BettingEventType"/> using
/// <see cref="DisplayAttribute"/> names and prefix matching for titles that append " - TeamName".
/// </summary>
public static class BookmakerEventTypeMapper
{
  private static readonly IReadOnlyList<(string DisplayName, BettingEventType Type)> DisplayToType;
  static BookmakerEventTypeMapper()
  {
    var list = new List<(string DisplayName, BettingEventType Type)>();
    foreach (BettingEventType value in Enum.GetValues(typeof(BettingEventType)))
    {
      var field = typeof(BettingEventType).GetField(value.ToString());
      var display = field?.GetCustomAttribute<DisplayAttribute>();
      var name = display?.GetName() ?? value.ToString();
      if (!string.IsNullOrWhiteSpace(name))
      {
        list.Add((name.Trim(), value));
      }
    }
    DisplayToType = list.OrderByDescending(x => x.DisplayName.Length).ToList();
  }

  /// <summary>
  /// Maps a bookmaker event title to the corresponding <see cref="BettingEventType"/>, or null if unmapped.
  /// </summary>
  public static BettingEventType? Map(string? title)
  {
    if (string.IsNullOrWhiteSpace(title))
    {
      return null;
    }

    var normalized = title.Trim();

    var comp = StringComparison.OrdinalIgnoreCase;

    foreach (var (displayName, type) in DisplayToType)
    {
      if (string.Equals(normalized, displayName, comp))
      {
        return type;
      }
      if (normalized.StartsWith(displayName, comp))
      {
        return type;
      }
    }

    return null;
  }
}
