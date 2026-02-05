using EduCheck.Infrastructure.Security.Events;

namespace EduCheck.Infrastructure.Security;

/// <summary>
/// Service for monitoring and logging security events.
/// </summary>
public interface ISecurityMonitoringService
{
    /// <summary>
    /// Logs a security event.
    /// </summary>
    Task LogSecurityEventAsync(SecurityEvent securityEvent);

    /// <summary>
    /// Records a failed login attempt and checks for brute force.
    /// </summary>
    Task<bool> RecordFailedLoginAsync(string? email, string? ipAddress, string? userAgent, string? reason);

    /// <summary>
    /// Records a successful login.
    /// </summary>
    Task RecordSuccessfulLoginAsync(string userId, string? email, string? role, string? ipAddress, string? userAgent);

    /// <summary>
    /// Checks if an IP is currently blocked.
    /// </summary>
    Task<bool> IsIpBlockedAsync(string ipAddress);

    /// <summary>
    /// Checks if an account is locked.
    /// </summary>
    Task<bool> IsAccountLockedAsync(string email);

    /// <summary>
    /// Records an IDOR attempt.
    /// </summary>
    Task RecordIdorAttemptAsync(string? userId, string resourceType, string resourceId, string? resourceOwnerId, string? ipAddress);

    /// <summary>
    /// Records a potential attack pattern.
    /// </summary>
    Task RecordAttackPatternAsync(SecurityEvent attackEvent);

    /// <summary>
    /// Gets the number of failed login attempts for an IP/email in a time window.
    /// </summary>
    Task<int> GetFailedLoginAttemptsAsync(string? ipAddress, string? email, TimeSpan window);
}