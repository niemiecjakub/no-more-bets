using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Tests.Common;

public class WallClockDateTimeTests
{
  private static readonly JsonSerializerOptions Options = CreateOptions();

  private static JsonSerializerOptions CreateOptions()
  {
    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    options.Converters.Add(new WallClockDateTime.JsonConverter());
    return options;
  }

  private sealed record Payload(
    [property: System.Text.Json.Serialization.JsonConverter(typeof(WallClockDateTime.JsonConverter))]
    DateTime MatchDate);

  [Fact]
  public void Write_UnspecifiedKind_SerializesWithoutTimezoneSuffix()
  {
    // Arrange
    var payload = new Payload(new DateTime(2026, 8, 24, 18, 0, 0, DateTimeKind.Unspecified));

    // Act
    var json = JsonSerializer.Serialize(payload, Options);

    // Assert
    json.Should().Contain("\"matchDate\":\"2026-08-24T18:00:00\"");
    json.Should().NotContain("Z");
  }
}
