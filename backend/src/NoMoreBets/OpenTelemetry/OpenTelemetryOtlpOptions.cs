using OpenTelemetry.Exporter;

namespace NoMoreBets.OpenTelemetry;

public sealed class OpenTelemetryOtlpOptions
{
  public const string SectionName = "OpenTelemetry:Otlp";

  public string? Endpoint { get; set; }

  public string Protocol { get; set; } = "grpc";

  public Dictionary<string, string>? Headers { get; set; }

  public static OtlpExportProtocol ParseProtocol(string? protocol) =>
    protocol?.Trim().ToLowerInvariant() switch
    {
      "http/protobuf" or "httpprotobuf" => OtlpExportProtocol.HttpProtobuf,
      _ => OtlpExportProtocol.Grpc
    };

  public static string? FormatHeaders(Dictionary<string, string>? headers)
  {
    if (headers is not { Count: > 0 })
    {
      return null;
    }

    return string.Join(",", headers.Select(pair =>
      $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
  }
}
