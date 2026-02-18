namespace EduCheck.Infrastructure.Security.Events;

/// <summary>
/// Event for SQL injection attempts.
/// </summary>
public class SqlInjectionAttemptEvent : SecurityEvent
{
    public override string Category => "Attack";
    public override string EventType => "SQLInjection";
    public override string Severity => "Critical";

    /// <summary>
    /// Parameter that contained the injection.
    /// </summary>
    public string? Parameter { get; set; }

    /// <summary>
    /// Detected pattern.
    /// </summary>
    public string? DetectedPattern { get; set; }

    /// <summary>
    /// Sanitized value (for logging without executing).
    /// </summary>
    public string? SanitizedValue { get; set; }

    /// <summary>
    /// Whether the request was blocked.
    /// </summary>
    public bool Blocked { get; set; } = true;
}

/// <summary>
/// Event for XSS attempts.
/// </summary>
public class XssAttemptEvent : SecurityEvent
{
    public override string Category => "Attack";
    public override string EventType => "XSSAttempt";
    public override string Severity => "High";

    public string? Parameter { get; set; }
    public string? DetectedPattern { get; set; }
    public bool Blocked { get; set; } = true;
}

/// <summary>
/// Event for path traversal attempts.
/// </summary>
public class PathTraversalAttemptEvent : SecurityEvent
{
    public override string Category => "Attack";
    public override string EventType => "PathTraversal";
    public override string Severity => "High";

    public string? AttemptedPath { get; set; }
    public string? DetectedPattern { get; set; }
    public bool Blocked { get; set; } = true;
}

/// <summary>
/// Event for rate limit violations.
/// </summary>
public class RateLimitExceededEvent : SecurityEvent
{
    public override string Category => "Attack";
    public override string EventType => "RateLimitExceeded";
    public override string Severity => "Medium";

    public string? Endpoint { get; set; }
    public int CurrentCount { get; set; }
    public int Limit { get; set; }
    public int WindowSeconds { get; set; }
}