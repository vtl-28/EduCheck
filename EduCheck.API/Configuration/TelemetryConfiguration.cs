using Grafana.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace EduCheck.API.Configuration;

/// <summary>
/// OpenTelemetry configuration using Grafana's official package.
/// IMPORTANT: This package reads from OTEL_* environment variables.
/// Use run-dev.sh script to set them automatically.
/// </summary>
public static class TelemetryConfiguration
{
    public static IServiceCollection AddTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        Console.WriteLine("=== GRAFANA OPENTELEMETRY SETUP ===");
        Console.WriteLine($"Service: {Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "NOT SET"}");
        Console.WriteLine($"Endpoint: {Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "NOT SET"}");
        Console.WriteLine("====================================");


        services.AddOpenTelemetry()
            .UseGrafana()
            .WithTracing(tracing => tracing
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.EnrichWithIDbCommand = (activity, command) =>
                    {
                        activity.SetTag("db.operation", command.CommandText);
                    };
                }))
            .WithMetrics(metrics => metrics
                .AddMeter("educheck-security")
                .AddMeter("educheck-api"));

        Console.WriteLine("[Grafana.OpenTelemetry] Configuration complete with EF Core instrumentation and security metrics!");

        return services;
    }

    public static ILoggingBuilder AddTelemetryLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration)
    {
        logging.AddOpenTelemetry(options => options.UseGrafana());
        return logging;
    }
}