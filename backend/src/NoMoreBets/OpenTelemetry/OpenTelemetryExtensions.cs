using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NoMoreBets.OpenTelemetry;

public static class OpenTelemetryExtensions
{
  public static WebApplicationBuilder AddOpenTelemetryObservability(this WebApplicationBuilder builder)
  {
    var otlpSection = builder.Configuration.GetSection(OpenTelemetryOtlpOptions.SectionName);

    var endpoint = otlpSection["Endpoint"] ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
    var protocol = otlpSection["Protocol"] ?? builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];

    if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(protocol))
    {
      return builder;
    }

    builder.Logging.AddOpenTelemetry(logging =>
    {
      logging.IncludeFormattedMessage = true;
      logging.IncludeScopes = true;
    });

    var otel = builder.Services.AddOpenTelemetry()
      .ConfigureResource(resource => resource.AddService(
        serviceName: builder.Configuration["OpenTelemetry:ServiceName"] ?? "NoMoreBets",
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString()));

    otel.WithTracing(tracing => tracing
      .AddAspNetCoreInstrumentation(options =>
      {
        options.Filter = ctx =>
          !ctx.Request.Path.StartsWithSegments("/health")
          && !ctx.Request.Path.StartsWithSegments("/hangfire");
      })
      .AddHttpClientInstrumentation()
      .AddEntityFrameworkCoreInstrumentation());

    otel.WithMetrics(metrics => metrics
      .AddAspNetCoreInstrumentation()
      .AddHttpClientInstrumentation()
      .AddRuntimeInstrumentation());

    var otlpOptions = otlpSection.Get<OpenTelemetryOtlpOptions>();
    var headers = OpenTelemetryOtlpOptions.FormatHeaders(otlpOptions?.Headers)
      ?? builder.Configuration["OTEL_EXPORTER_OTLP_HEADERS"];

    builder.Services.Configure<OtlpExporterOptions>(options =>
    {
      options.Endpoint = new Uri(endpoint, UriKind.Absolute);
      options.Protocol = OpenTelemetryOtlpOptions.ParseProtocol(protocol);
      if (!string.IsNullOrWhiteSpace(headers))
      {
        options.Headers = headers;
      }
    });

    otel.UseOtlpExporter();

    return builder;
  }
}
