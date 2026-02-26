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
        var typeName = exception.GetType().FullName ?? string.Empty;

        return exception switch
        {
            OperationCanceledException or TaskCanceledException =>
                (HttpStatusCode.ServiceUnavailable,
                 "The request was cancelled. Please try again."),

            HttpRequestException =>
                (HttpStatusCode.ServiceUnavailable,
                 "A network error occurred. Please check your connection and try again."),

            KeyNotFoundException =>
                (HttpStatusCode.NotFound,
                 "The requested resource was not found."),

            UnauthorizedAccessException =>
                (HttpStatusCode.Unauthorized,
                 "You are not authorised to perform this action."),

            ArgumentNullException or ArgumentException =>
                (HttpStatusCode.BadRequest,
                 "The request could not be processed. Please check your input and try again."),

            InvalidOperationException =>
                (HttpStatusCode.BadRequest,
                 "The operation could not be completed. Please try again."),

            _ when typeName.Contains("Npgsql") ||
                   typeName.Contains("DbUpdate") ||
                   typeName.Contains("DbUpdateConcurrency") =>
                (HttpStatusCode.ServiceUnavailable,
                 "A database error occurred. Please try again in a moment."),

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