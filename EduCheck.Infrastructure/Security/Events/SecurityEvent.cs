namespace EduCheck.Infrastructure.Security.Events;

/// <summary>
/// Base class for all security events.
/// </summary>
public abstract class SecurityEvent
{
    /// <summary>
    /// Unique identifier for this event.
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Category of security event.
    /// </summary>
    public abstract string Category { get; }

    /// <summary>
    /// Type of security event.
    /// </summary>
    public abstract string EventType { get; }

    /// <summary>
    /// Severity level: Low, Medium, High, Critical
    /// </summary>
    public abstract string Severity { get; }

    /// <summary>
    /// IP address of the request.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// User ID if authenticated.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Request path.
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// HTTP method.
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; set; }
}