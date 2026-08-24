using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NoMoreBets.Application.Common;

/// <summary>
/// Wall-clock kickoff times (no real timezone). Serialized without a Z suffix.
/// </summary>
public static class WallClockDateTime
{
  public sealed class JsonConverter : JsonConverter<DateTime>
  {
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
      DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Unspecified);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
      writer.WriteStringValue(value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture));
  }
}
