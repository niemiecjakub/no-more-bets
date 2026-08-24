using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NoMoreBets.Application.Common;

/// <summary>
/// UTC DateTime contract: Unspecified Kind is UTC (Postgres timestamps), Local is converted.
/// JSON converters emit ISO-8601 with a trailing Z.
/// </summary>
public static class UtcDateTime
{
  public static DateTime ToUtc(DateTime value) =>
    value.Kind switch
    {
      DateTimeKind.Utc => value,
      DateTimeKind.Local => value.ToUniversalTime(),
      _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

  public sealed class JsonConverter : JsonConverter<DateTime>
  {
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
      ToUtc(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
      writer.WriteStringValue(ToUtc(value).ToString("o", CultureInfo.InvariantCulture));
  }

  public sealed class NullableJsonConverter : JsonConverter<DateTime?>
  {
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      if (reader.TokenType == JsonTokenType.Null)
        return null;

      return ToUtc(reader.GetDateTime());
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
      if (value is null)
      {
        writer.WriteNullValue();
        return;
      }

      writer.WriteStringValue(ToUtc(value.Value).ToString("o", CultureInfo.InvariantCulture));
    }
  }
}
