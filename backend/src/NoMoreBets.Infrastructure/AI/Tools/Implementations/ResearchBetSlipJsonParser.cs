using System.Text.Json;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Infrastructure.AI.Tools.Implementations;

internal static class ResearchBetSlipJsonParser
{
  internal sealed record Selection(BettingEventType EventType, BettingEventOption Option);

  internal static bool TryParse(
    string betSelectionsJson,
    out List<Selection> betSelections,
    out string? error)
  {
    betSelections = [];
    error = null;

    JsonDocument document;
    try
    {
      document = JsonDocument.Parse(betSelectionsJson);
    }
    catch (JsonException ex)
    {
      error =
        "Invalid betSelections JSON. Expected {\"betSelections\":[{\"eventType\":\"BothTeamsToScore\",\"eventOption\":\"BothTeamsToScore_Yes\"}]} "
        + $"or a bare array of selection objects. Parse error: {ex.Message}";
      return false;
    }

    using (document)
    {
      var root = document.RootElement;
      if (!TryGetSelectionsArray(root, out var selectionsArray, out error))
      {
        return false;
      }

      foreach (var item in selectionsArray.EnumerateArray())
      {
        if (!TryReadSelection(item, out var record, out error))
        {
          betSelections = [];
          return false;
        }

        betSelections.Add(record);
      }
    }

    if (betSelections.Count == 0)
    {
      error = "At least one selection is required to place a research bet slip.";
      return false;
    }

    return true;
  }

  private static bool TryGetSelectionsArray(JsonElement root, out JsonElement selectionsArray, out string? error)
  {
    error = null;
    selectionsArray = default;

    if (root.ValueKind == JsonValueKind.Array)
    {
      selectionsArray = root;
      return true;
    }

    if (root.ValueKind == JsonValueKind.Object
      && root.TryGetProperty("betSelections", out var property)
      && property.ValueKind == JsonValueKind.Array)
    {
      selectionsArray = property;
      return true;
    }

    error =
      "Invalid betSelections JSON. Expected an object with betSelections array or a bare array of selection objects. "
      + "Each selection needs eventType and eventOption (enum names from GetMatchEvents).";
    return false;
  }

  private static bool TryReadSelection(JsonElement item, out Selection record, out string? error)
  {
    record = default!;
    error = null;

    if (item.ValueKind != JsonValueKind.Object)
    {
      error = "Each bet selection must be a JSON object with eventType and eventOption.";
      return false;
    }

    if (!TryGetEnumProperty(item, "eventType", out BettingEventType eventType))
    {
      error = "Each selection must include eventType (enum name from GetMatchEvents eventTypeName, e.g. BothTeamsToScore).";
      return false;
    }

    if (!TryGetEnumProperty(item, "eventOption", out BettingEventOption option)
      && !TryGetEnumProperty(item, "option", out option))
    {
      error = "Each selection must include eventOption (enum name from GetMatchEvents options, e.g. BothTeamsToScore_Yes).";
      return false;
    }

    record = new Selection(eventType, option);
    return true;
  }

  private static bool TryGetEnumProperty<TEnum>(JsonElement item, string propertyName, out TEnum value)
    where TEnum : struct, Enum
  {
    value = default;
    foreach (var property in item.EnumerateObject())
    {
      if (!property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      if (property.Value.ValueKind != JsonValueKind.String)
      {
        return false;
      }

      return Enum.TryParse(property.Value.GetString(), ignoreCase: true, out value);
    }

    return false;
  }
}
