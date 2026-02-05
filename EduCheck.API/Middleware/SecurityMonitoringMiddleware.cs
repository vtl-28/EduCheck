using EduCheck.API.Metrics;
using EduCheck.Infrastructure.Security;
using EduCheck.Infrastructure.Security.Events;

namespace EduCheck.API.Middleware;

/// <summary>
/// Middleware that monitors requests for security threats.
/// </summary>
public class SecurityMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityMonitoringMiddleware> _logger;
    private readonly AttackPatternDetector _attackDetector;

    public SecurityMonitoringMiddleware(
        RequestDelegate next,
        ILogger<SecurityMonitoringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _attackDetector = new AttackPatternDetector();
    }

    public async Task InvokeAsync(
        HttpContext context,
        ISecurityMonitoringService securityService,
        SecurityMetrics securityMetrics)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var path = context.Request.Path.Value ?? "";

        // Skip security checks for health and metrics endpoints
        if (path.Contains("/health") || path.Contains("/metrics") || path.Contains("/swagger"))
        {
            await _next(context);
            return;
        }

        // Check if IP is blocked
        if (!string.IsNullOrEmpty(ipAddress) && await securityService.IsIpBlockedAsync(ipAddress))
        {
            _logger.LogWarning("Blocked IP attempted access: {IpAddress}", ipAddress);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                Success = false,
                Message = "Access denied. Please try again later.",
                Errors = new[] { "Your IP has been temporarily blocked due to suspicious activity." }
            });
            return;
        }

        // Check query string for attack patterns
        foreach (var (key, value) in context.Request.Query)
        {
            var attackEvent = await CheckForAttackPatterns(value.ToString(), key, context, securityService, securityMetrics);
            if (attackEvent != null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    Success = false,
                    Message = "Invalid request detected.",
                    Errors = new[] { "Your request contained invalid characters." }
                });
                return;
            }
        }

        // Check path for traversal attempts
        var pathTraversal = _attackDetector.DetectPathTraversal(path);
        if (pathTraversal != null)
        {
            pathTraversal.IpAddress = ipAddress;
            pathTraversal.RequestPath = path;
            pathTraversal.UserAgent = context.Request.Headers.UserAgent.ToString();
            
            await securityService.RecordAttackPatternAsync(pathTraversal);
            securityMetrics.RecordAttackPattern("PathTraversal");

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                Success = false,
                Message = "Invalid request path."
            });
            return;
        }

        await _next(context);
    }

    private async Task<SecurityEvent?> CheckForAttackPatterns(
        string value,
        string parameter,
        HttpContext context,
        ISecurityMonitoringService securityService,
        SecurityMetrics securityMetrics)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var path = context.Request.Path.Value;

        // Check for SQL injection
        var sqlInjection = _attackDetector.DetectSqlInjection(value, parameter);
        if (sqlInjection != null)
        {
            sqlInjection.IpAddress = ipAddress;
            sqlInjection.UserAgent = userAgent;
            sqlInjection.RequestPath = path;
            
            await securityService.RecordAttackPatternAsync(sqlInjection);
            securityMetrics.RecordAttackPattern("SQLInjection");
            
            _logger.LogError(
                "SQL Injection attempt detected. IP: {IpAddress}, Parameter: {Parameter}, Path: {Path}",
                ipAddress, parameter, path);
            
            return sqlInjection;
        }

        // Check for XSS
        var xss = _attackDetector.DetectXss(value, parameter);
        if (xss != null)
        {
            xss.IpAddress = ipAddress;
            xss.UserAgent = userAgent;
            xss.RequestPath = path;
            
            await securityService.RecordAttackPatternAsync(xss);
            securityMetrics.RecordAttackPattern("XSS");
            
            _logger.LogError(
                "XSS attempt detected. IP: {IpAddress}, Parameter: {Parameter}, Path: {Path}",
                ipAddress, parameter, path);
            
            return xss;
        }

        return null;
    }
}

public static class SecurityMonitoringMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityMonitoring(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityMonitoringMiddleware>();
    }
}