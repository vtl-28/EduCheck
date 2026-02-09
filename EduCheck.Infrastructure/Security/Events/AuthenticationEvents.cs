namespace EduCheck.Infrastructure.Security.Events;

/// <summary>
/// Event for failed login attempts.
/// </summary>
public class LoginFailedEvent : SecurityEvent
{
    public override string Category => "Authentication";
    public override string EventType => "LoginFailed";
    public override string Severity => "Medium";

    /// <summary>
    /// Email address used in the attempt.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Reason for failure.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Number of failed attempts from this IP/email combination.
    /// </summary>
    public int AttemptCount { get; set; }
}

/// <summary>
/// Event for successful login.
/// </summary>
public class LoginSuccessEvent : SecurityEvent
{
    public override string Category => "Authentication";
    public override string EventType => "LoginSuccess";
    public override string Severity => "Low";

    public string? Email { get; set; }
    public string? UserRole { get; set; }
}

/// <summary>
/// Event for brute force detection.
/// </summary>
public class BruteForceDetectedEvent : SecurityEvent
{
    public override string Category => "Authentication";
    public override string EventType => "BruteForceDetected";
    public override string Severity => "Critical";

    /// <summary>
    /// Number of failed attempts in the time window.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Time window in minutes.
    /// </summary>
    public int TimeWindowMinutes { get; set; }

    /// <summary>
    /// Targeted email addresses.
    /// </summary>
    public List<string> TargetedEmails { get; set; } = new();
}

/// <summary>
/// Event for account lockout.
/// </summary>
public class AccountLockedEvent : SecurityEvent
{
    public override string Category => "Authentication";
    public override string EventType => "AccountLocked";
    public override string Severity => "High";

    public string? Email { get; set; }
    public int FailedAttempts { get; set; }
    public int LockoutDurationMinutes { get; set; }
}

/// <summary>
/// Event for invalid token usage.
/// </summary>
public class InvalidTokenEvent : SecurityEvent
{
    public override string Category => "Authentication";
    public override string EventType => "InvalidToken";
    public override string Severity => "Medium";

    public string? TokenType { get; set; }
    public string? Reason { get; set; }
}