using System.Collections.Concurrent;
using EduCheck.Infrastructure.Security.Events;
using Microsoft.Extensions.Logging;

namespace EduCheck.Infrastructure.Security;

/// <summary>
/// Implementation of security monitoring service.
/// Uses in-memory storage for tracking attempts (use Redis in production).
/// </summary>
public class SecurityMonitoringService : ISecurityMonitoringService
{
    private readonly ILogger<SecurityMonitoringService> _logger;
    
    // In-memory tracking (replace with Redis in production)
    private readonly ConcurrentDictionary<string, List<DateTime>> _failedLoginsByIp = new();
    private readonly ConcurrentDictionary<string, List<DateTime>> _failedLoginsByEmail = new();
    private readonly ConcurrentDictionary<string, DateTime> _blockedIps = new();
    private readonly ConcurrentDictionary<string, DateTime> _lockedAccounts = new();

    // Thresholds
    private const int MaxFailedAttemptsPerIp = 10;
    private const int MaxFailedAttemptsPerEmail = 5;
    private const int TimeWindowMinutes = 5;
    private const int BlockDurationMinutes = 15;
    private const int LockoutDurationMinutes = 30;

    public SecurityMonitoringService(ILogger<SecurityMonitoringService> logger)
    {
        _logger = logger;
    }

    public Task LogSecurityEventAsync(SecurityEvent securityEvent)
    {
        // Log with structured data for Loki/Grafana
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["SecurityEventId"] = securityEvent.EventId,
            ["SecurityCategory"] = securityEvent.Category,
            ["SecurityEventType"] = securityEvent.EventType,
            ["SecuritySeverity"] = securityEvent.Severity,
            ["IpAddress"] = securityEvent.IpAddress,
            ["UserId"] = securityEvent.UserId,
            ["RequestPath"] = securityEvent.RequestPath,
            ["CorrelationId"] = securityEvent.CorrelationId
        }))
        {
            var logLevel = securityEvent.Severity switch
            {
                "Critical" => LogLevel.Critical,
                "High" => LogLevel.Error,
                "Medium" => LogLevel.Warning,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel, 
                "SECURITY: [{Category}] {EventType} - {Severity} severity from IP {IpAddress}",
                securityEvent.Category,
                securityEvent.EventType,
                securityEvent.Severity,
                securityEvent.IpAddress);
        }

        return Task.CompletedTask;
    }

    public async Task<bool> RecordFailedLoginAsync(string? email, string? ipAddress, string? userAgent, string? reason)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(-TimeWindowMinutes);
        var bruteForceDetected = false;

        // Prepare events outside locks
        BruteForceDetectedEvent? ipBruteForceEvent = null;
        AccountLockedEvent? accountLockedEvent = null;

        // Track by IP
        if (!string.IsNullOrEmpty(ipAddress))
        {
            var ipAttempts = _failedLoginsByIp.GetOrAdd(ipAddress, _ => new List<DateTime>());
            lock (ipAttempts)
            {
                ipAttempts.RemoveAll(t => t < cutoff);
                ipAttempts.Add(now);

                if (ipAttempts.Count >= MaxFailedAttemptsPerIp)
                {
                    bruteForceDetected = true;
                    _blockedIps[ipAddress] = now.AddMinutes(BlockDurationMinutes);
                    
                    // Prepare event data inside lock
                    ipBruteForceEvent = new BruteForceDetectedEvent
                    {
                        IpAddress = ipAddress,
                        UserAgent = userAgent,
                        AttemptCount = ipAttempts.Count,
                        TimeWindowMinutes = TimeWindowMinutes,
                        TargetedEmails = GetTargetedEmails(ipAddress)
                    };
                }
            }
            
            // Log outside the lock
            if (ipBruteForceEvent != null)
            {
                await LogSecurityEventAsync(ipBruteForceEvent);
            }
        }

        // Track by email
        if (!string.IsNullOrEmpty(email))
        {
            var emailAttempts = _failedLoginsByEmail.GetOrAdd(email.ToLower(), _ => new List<DateTime>());
            lock (emailAttempts)
            {
                emailAttempts.RemoveAll(t => t < cutoff);
                emailAttempts.Add(now);

                if (emailAttempts.Count >= MaxFailedAttemptsPerEmail)
                {
                    _lockedAccounts[email.ToLower()] = now.AddMinutes(LockoutDurationMinutes);
                    
                    // Prepare event data inside lock
                    accountLockedEvent = new AccountLockedEvent
                    {
                        Email = email,
                        IpAddress = ipAddress,
                        UserAgent = userAgent,
                        FailedAttempts = emailAttempts.Count,
                        LockoutDurationMinutes = LockoutDurationMinutes
                    };
                }
            }
            
            // Log outside the lock
            if (accountLockedEvent != null)
            {
                await LogSecurityEventAsync(accountLockedEvent);
            }
        }

        // Log the failed attempt
        var attemptCount = await GetFailedLoginAttemptsAsync(ipAddress, email, TimeSpan.FromMinutes(TimeWindowMinutes));
        await LogSecurityEventAsync(new LoginFailedEvent
        {
            Email = email,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            FailureReason = reason,
            AttemptCount = attemptCount
        });

        return bruteForceDetected;
    }

    public Task RecordSuccessfulLoginAsync(string userId, string? email, string? role, string? ipAddress, string? userAgent)
    {
        // Clear failed attempts on successful login
        if (!string.IsNullOrEmpty(email))
        {
            _failedLoginsByEmail.TryRemove(email.ToLower(), out _);
            _lockedAccounts.TryRemove(email.ToLower(), out _);
        }

        return LogSecurityEventAsync(new LoginSuccessEvent
        {
            UserId = userId,
            Email = email,
            UserRole = role,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });
    }

    public Task<bool> IsIpBlockedAsync(string ipAddress)
    {
        if (_blockedIps.TryGetValue(ipAddress, out var blockedUntil))
        {
            if (DateTime.UtcNow < blockedUntil)
            {
                return Task.FromResult(true);
            }
            _blockedIps.TryRemove(ipAddress, out _);
        }
        return Task.FromResult(false);
    }

    public Task<bool> IsAccountLockedAsync(string email)
    {
        if (_lockedAccounts.TryGetValue(email.ToLower(), out var lockedUntil))
        {
            if (DateTime.UtcNow < lockedUntil)
            {
                return Task.FromResult(true);
            }
            _lockedAccounts.TryRemove(email.ToLower(), out _);
        }
        return Task.FromResult(false);
    }

    public Task RecordIdorAttemptAsync(string? userId, string resourceType, string resourceId, string? resourceOwnerId, string? ipAddress)
    {
        return LogSecurityEventAsync(new IdorAttemptEvent
        {
            UserId = userId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            ResourceOwnerId = resourceOwnerId,
            IpAddress = ipAddress,
            AttemptedAction = "Access"
        });
    }

    public Task RecordAttackPatternAsync(SecurityEvent attackEvent)
    {
        return LogSecurityEventAsync(attackEvent);
    }

    public Task<int> GetFailedLoginAttemptsAsync(string? ipAddress, string? email, TimeSpan window)
    {
        var cutoff = DateTime.UtcNow - window;
        var count = 0;

        if (!string.IsNullOrEmpty(ipAddress) && _failedLoginsByIp.TryGetValue(ipAddress, out var ipAttempts))
        {
            lock (ipAttempts)
            {
                count += ipAttempts.Count(t => t >= cutoff);
            }
        }

        if (!string.IsNullOrEmpty(email) && _failedLoginsByEmail.TryGetValue(email.ToLower(), out var emailAttempts))
        {
            lock (emailAttempts)
            {
                count += emailAttempts.Count(t => t >= cutoff);
            }
        }

        return Task.FromResult(count);
    }

    private List<string> GetTargetedEmails(string ipAddress)
    {
        // This would need to track email-IP associations
        // Simplified for now
        return new List<string>();
    }
}