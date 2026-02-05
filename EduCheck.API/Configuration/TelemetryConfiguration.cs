using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EduCheck.API.Configuration;

/// <summary>
/// OpenTelemetry configuration for logs, metrics, and traces.
/// </summary>
public static class TelemetryConfiguration
{
    private const string ServiceName = "educheck-api";
    private const string ServiceVersion = "1.0.0";

    /// <summary>
    /// Adds OpenTelemetry services for logging, metrics, and tracing.
    /// </summary>
    public static IServiceCollection AddTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4318";

        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: ServiceName,
                    serviceVersion: ServiceVersion,
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development",
                    ["host.name"] = Environment.MachineName
                }))
            .WithTracing(tracing =>
            {
                tracing
                    // Add sources for manual instrumentation
                    .AddSource(ServiceName)
                    
                    // Auto-instrument ASP.NET Core
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                        {
                            // Don't trace health checks and metrics endpoints
                            var path = httpContext.Request.Path.Value ?? "";
                            return !path.Contains("/health") && 
                                   !path.Contains("/metrics") &&
                                   !path.Contains("/swagger");
                        };
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response_content_length", response.ContentLength);
                        };
                    })
                    
                    // Auto-instrument HTTP client calls
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    
                    // Auto-instrument Entity Framework Core
                    .AddEntityFrameworkCoreInstrumentation()
                    
                    // Export to OTel Collector
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri($"{otlpEndpoint}/v1/traces");
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    // Add custom meters
                    .AddMeter(ServiceName)
                    
                    // Auto-instrument ASP.NET Core
                    .AddAspNetCoreInstrumentation()
                    
                    // Auto-instrument HTTP client
                    .AddHttpClientInstrumentation()
                    
                    // Add runtime metrics (GC, threads, etc.)
                    .AddRuntimeInstrumentation()
                    
                    // Export to Prometheus endpoint
                    .AddPrometheusExporter()
                    
                    // Also export to OTel Collector
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri($"{otlpEndpoint}/v1/metrics");
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                    });
            });

        return services;
    }

    /// <summary>
    /// Configures OpenTelemetry logging.
    /// </summary>
    public static ILoggingBuilder AddTelemetryLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4318";

        logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            
            options.SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(ServiceName, serviceVersion: ServiceVersion));

            options.AddOtlpExporter(exporterOptions =>
            {
                exporterOptions.Endpoint = new Uri($"{otlpEndpoint}/v1/logs");
                exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            });
        });

        return logging;
    }
}