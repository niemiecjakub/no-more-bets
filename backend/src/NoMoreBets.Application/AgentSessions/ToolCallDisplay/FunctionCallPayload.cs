using System.Text.Json;

namespace NoMoreBets.Application.AgentSessions.ToolCallDisplay;

public sealed record FunctionCallPayload(string Name, IReadOnlyList<FunctionCallArgument>? Arguments);

public sealed record FunctionCallArgument(string Name, string? Value);

public static class FunctionCallPayloadParser
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public static bool TryParse(string text, out FunctionCallPayload payload)
  {
    payload = null!;
    if (string.IsNullOrWhiteSpace(text))
      return false;

    try
    {
      using var document = JsonDocument.Parse(text);
      var root = document.RootElement;
      if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("name", out var nameElement))
        return false;

      var name = nameElement.GetString();
      if (string.IsNullOrWhiteSpace(name))
        return false;

      IReadOnlyList<FunctionCallArgument>? arguments = null;
      if (root.TryGetProperty("arguments", out var argumentsElement) && argumentsElement.ValueKind == JsonValueKind.Array)
      {
        var parsedArguments = new List<FunctionCallArgument>();
        foreach (var argumentElement in argumentsElement.EnumerateArray())
        {
          if (argumentElement.ValueKind != JsonValueKind.Object
            || !argumentElement.TryGetProperty("name", out var argumentNameElement))
          {
            continue;
          }

          var argumentName = argumentNameElement.GetString();
          if (string.IsNullOrWhiteSpace(argumentName))
            continue;

          string? argumentValue = null;
          if (argumentElement.TryGetProperty("value", out var valueElement)
            && valueElement.ValueKind != JsonValueKind.Null)
          {
            argumentValue = valueElement.ValueKind switch
            {
              JsonValueKind.String => valueElement.GetString(),
              _ => valueElement.GetRawText(),
            };
          }

          parsedArguments.Add(new FunctionCallArgument(argumentName, argumentValue));
        }

        arguments = parsedArguments;
      }

      payload = new FunctionCallPayload(name, arguments);
      return true;
    }
    catch (JsonException)
    {
      return false;
    }
  }

  public static JsonElement? GetArgumentValue(FunctionCallPayload payload, string argumentName)
  {
    var argument = payload.Arguments?.FirstOrDefault(a =>
      string.Equals(a.Name, argumentName, StringComparison.Ordinal));
    if (argument?.Value is not { } rawValue || rawValue.Length == 0)
      return null;

    try
    {
      using var document = JsonDocument.Parse(rawValue);
      return document.RootElement.Clone();
    }
    catch (JsonException)
    {
      return JsonSerializer.SerializeToElement(rawValue, JsonOptions);
    }
  }

  public static int? ParsePositiveInt(JsonElement? value)
  {
    if (value is not { } element)
      return null;

    int? parsed = element.ValueKind switch
    {
      JsonValueKind.Number when element.TryGetInt32(out var number) => number,
      JsonValueKind.String when int.TryParse(element.GetString(), out var number) => number,
      _ => null,
    };

    return parsed is > 0 ? parsed : null;
  }

  public static string? ParseString(JsonElement? value)
  {
    if (value is not { } element)
      return null;

    return element.ValueKind switch
    {
      JsonValueKind.String => element.GetString()?.Trim(),
      JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => element.GetRawText(),
      _ => null,
    };
  }

  public static bool ParseBooleanTrue(JsonElement? value)
  {
    if (value is not { } element)
      return false;

    return element.ValueKind switch
    {
      JsonValueKind.True => true,
      JsonValueKind.String => string.Equals(element.GetString(), "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(element.GetString(), "True", StringComparison.Ordinal),
      _ => false,
    };
  }
}
