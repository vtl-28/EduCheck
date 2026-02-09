namespace EduCheck.Infrastructure.Security.Events;

/// <summary>
/// Event for IDOR (Insecure Direct Object Reference) attempts.
/// </summary>
public class IdorAttemptEvent : SecurityEvent
{
    public override string Category => "Authorization";
    public override string EventType => "IDORAttempt";
    public override string Severity => "High";

    /// <summary>
    /// Type of resource being accessed.
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// ID of the resource being accessed.
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Owner of the resource.
    /// </summary>
    public string? ResourceOwnerId { get; set; }

    /// <summary>
    /// Action attempted.
    /// </summary>
    public string? AttemptedAction { get; set; }
}

/// <summary>
/// Event for privilege escalation attempts.
/// </summary>
public class PrivilegeEscalationEvent : SecurityEvent
{
    public override string Category => "Authorization";
    public override string EventType => "PrivilegeEscalation";
    public override string Severity => "Critical";

    /// <summary>
    /// Current role of the user.
    /// </summary>
    public string? CurrentRole { get; set; }

    /// <summary>
    /// Role or permission being attempted.
    /// </summary>
    public string? AttemptedRole { get; set; }

    /// <summary>
    /// Endpoint being accessed.
    /// </summary>
    public string? Endpoint { get; set; }
}

/// <summary>
/// Event for access denied.
/// </summary>
public class AccessDeniedEvent : SecurityEvent
{
    public override string Category => "Authorization";
    public override string EventType => "AccessDenied";
    public override string Severity => "Medium";

    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public string? RequiredPermission { get; set; }
    public string? Reason { get; set; }
}