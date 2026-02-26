using System.Diagnostics;
using System.Security.Claims;

namespace EduCheck.API.Middleware;

/// <summary>
/// Middleware that enriches telemetry with request context.
/// Adds user info, correlation IDs, and other contextual data.
/// </summary>
public class TelemetryEnrichmentMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TelemetryEnrichmentMiddleware> _logger;

    public TelemetryEnrichmentMiddleware(
        RequestDelegate next,
        ILogger<TelemetryEnrichmentMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                            ?? Activity.Current?.TraceId.ToString()
                            ?? Guid.NewGuid().ToString();

        context.Response.Headers["X-Correlation-ID"] = correlationId;

        var activity = Activity.Current;

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? context.User.FindFirst("sub")?.Value;
        var userEmail = context.User.FindFirst(ClaimTypes.Email)?.Value;
        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

        if (activity != null)
        {
            activity.SetTag("correlation_id", correlationId);

            if (!string.IsNullOrEmpty(userId))
            {
                activity.SetTag("user.id", userId);
            }
            if (!string.IsNullOrEmpty(userRole))
            {
                activity.SetTag("user.role", userRole);
            }

            activity.SetTag("http.client_ip", context.Connection.RemoteIpAddress?.ToString());
            activity.SetTag("http.user_agent", context.Request.Headers.UserAgent.ToString());
        }

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = userId,
            ["UserEmail"] = userEmail,
            ["UserRole"] = userRole,
            ["ClientIp"] = context.Connection.RemoteIpAddress?.ToString(),
            ["UserAgent"] = context.Request.Headers.UserAgent.ToString(),
            ["RequestPath"] = context.Request.Path.Value,
            ["RequestMethod"] = context.Request.Method
        }))
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Extension method to add the middleware.
/// </summary>
public static class TelemetryEnrichmentMiddlewareExtensions
{
    public static IApplicationBuilder UseTelemetryEnrichment(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TelemetryEnrichmentMiddleware>();
    }
}