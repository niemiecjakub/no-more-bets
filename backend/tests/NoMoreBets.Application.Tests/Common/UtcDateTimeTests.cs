using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Tests.Common;

public class UtcDateTimeTests
{
  private static readonly JsonSerializerOptions Options = CreateOptions();

  private static JsonSerializerOptions CreateOptions()
  {
    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    options.Converters.Add(new UtcDateTime.JsonConverter());
    options.Converters.Add(new UtcDateTime.NullableJsonConverter());
    return options;
  }

  private sealed record Payload(DateTime StartedAt);

  private sealed record NullablePayload(DateTime? StartedAt);

  [Fact]
  public void ToUtc_UnspecifiedKind_TreatsAsUtc()
  {
    // Arrange
    var unspecified = new DateTime(2026, 8, 24, 13, 0, 0, DateTimeKind.Unspecified);

    // Act
    var utc = UtcDateTime.ToUtc(unspecified);

    // Assert
    utc.Kind.Should().Be(DateTimeKind.Utc);
    utc.Should().Be(new DateTime(2026, 8, 24, 13, 0, 0, DateTimeKind.Utc));
  }

  [Fact]
  public void Write_UnspecifiedKind_SerializesWithTrailingZ()
  {
    // Arrange
    var payload = new Payload(new DateTime(2026, 8, 24, 13, 0, 0, DateTimeKind.Unspecified));

    // Act
    var json = JsonSerializer.Serialize(payload, Options);

    // Assert
    json.Should().Contain("\"startedAt\":\"2026-08-24T13:00:00.0000000Z\"");
  }

  [Fact]
  public void Write_UtcKind_SerializesWithTrailingZ()
  {
    // Arrange
    var payload = new Payload(new DateTime(2026, 8, 24, 13, 0, 0, DateTimeKind.Utc));

    // Act
    var json = JsonSerializer.Serialize(payload, Options);

    // Assert
    json.Should().Contain("\"startedAt\":\"2026-08-24T13:00:00.0000000Z\"");
  }

  [Fact]
  public void Write_SerializedString_ParsesAsUtcInstantInJsStyle()
  {
    // Arrange
    var payload = new Payload(new DateTime(2026, 8, 24, 13, 0, 0, DateTimeKind.Unspecified));
    var json = JsonSerializer.Serialize(payload, Options);
    using var doc = JsonDocument.Parse(json);
    var iso = doc.RootElement.GetProperty("startedAt").GetString();

    // Act — same contract browsers use for ISO with Z
    var parsed = DateTimeOffset.Parse(iso!, CultureInfo.InvariantCulture);

    // Assert
    parsed.UtcDateTime.Should().Be(new DateTime(2026, 8, 24, 13, 0, 0, DateTimeKind.Utc));
    iso.Should().EndWith("Z");
  }

  [Fact]
  public void Write_NullNullable_SerializesNull()
  {
    // Arrange
    var payload = new NullablePayload(null);

    // Act
    var json = JsonSerializer.Serialize(payload, Options);

    // Assert
    json.Should().Contain("\"startedAt\":null");
  }

  [Fact]
  public void Read_IsoWithZ_ReturnsUtc()
  {
    // Arrange
    const string json = """{"startedAt":"2026-08-24T13:00:00Z"}""";

    // Act
    var payload = JsonSerializer.Deserialize<Payload>(json, Options);

    // Assert
    payload.Should().NotBeNull();
    payload!.StartedAt.Kind.Should().Be(DateTimeKind.Utc);
    payload.StartedAt.Should().Be(new DateTime(2026, 8, 24, 13, 0, 0, DateTimeKind.Utc));
  }
}
