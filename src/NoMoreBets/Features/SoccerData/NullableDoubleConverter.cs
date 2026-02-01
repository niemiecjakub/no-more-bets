using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NoMoreBets.Features.SoccerData;

/// <summary>
/// JSON converter for <see cref="double?"/> that accepts both JSON number and string
/// (e.g. SoccerData API may send odds values as "+0.0" or 2.5).
/// </summary>
public sealed class NullableDoubleConverter : JsonConverter<double?>
{
    /// <inheritdoc />
    public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String => double.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null,
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Unexpected token {reader.TokenType} when parsing double?.")
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}
