using System.Net;
using System.Text.Json;

namespace EduCheck.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Always log the full exception server-side — never expose internals to the client
        _logger.LogError(exception,
            "Unhandled exception on {Method} {Path} — TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier);

        var (statusCode, message) = GetStatusAndMessage(exception);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            message,
            traceId = context.TraceIdentifier,
            // Only include exception detail in development to help debugging
            detail = _env.IsDevelopment() ? exception.Message : (string?)null
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static (HttpStatusCode statusCode, string message) GetStatusAndMessage(Exception exception)
    {
        // Check exception type name to avoid direct Npgsql/EF references in API layer
        var typeName = exception.GetType().FullName ?? string.Empty;

        return exception switch
        {
            // Cancellation — user navigated away or request timed out
            OperationCanceledException or TaskCanceledException =>
                (HttpStatusCode.ServiceUnavailable,
                 "The request was cancelled. Please try again."),

            // Network issues
            HttpRequestException =>
                (HttpStatusCode.ServiceUnavailable,
                 "A network error occurred. Please check your connection and try again."),

            // Not found — thrown explicitly in services
            KeyNotFoundException =>
                (HttpStatusCode.NotFound,
                 "The requested resource was not found."),

            // Unauthorised access
            UnauthorizedAccessException =>
                (HttpStatusCode.Unauthorized,
                 "You are not authorised to perform this action."),

            // Bad input that slipped past validation
            ArgumentNullException or ArgumentException =>
                (HttpStatusCode.BadRequest,
                 "The request could not be processed. Please check your input and try again."),

            // Invalid state
            InvalidOperationException =>
                (HttpStatusCode.BadRequest,
                 "The operation could not be completed. Please try again."),

            // Database / Npgsql exceptions (matched by type name to avoid project reference)
            _ when typeName.Contains("Npgsql") ||
                   typeName.Contains("DbUpdate") ||
                   typeName.Contains("DbUpdateConcurrency") =>
                (HttpStatusCode.ServiceUnavailable,
                 "A database error occurred. Please try again in a moment."),

            // Everything else — never leak internals
            _ =>
                (HttpStatusCode.InternalServerError,
                 "An unexpected error occurred. Please try again later.")
        };
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}