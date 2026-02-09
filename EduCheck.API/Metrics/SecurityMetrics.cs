using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EduCheck.API.Metrics;

/// <summary>
/// Security-related metrics for monitoring.
/// </summary>
public class SecurityMetrics
{
    private readonly Counter<long> _authFailures;
    private readonly Counter<long> _authSuccesses;
    private readonly Counter<long> _idorAttempts;
    private readonly Counter<long> _attackPatterns;
    private readonly Counter<long> _rateLimitHits;
    private readonly Counter<long> _accountLockouts;

    public SecurityMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("educheck-security");

        _authFailures = meter.CreateCounter<long>(
            name: "security.auth.failures",
            unit: "{failures}",
            description: "Number of authentication failures");

        _authSuccesses = meter.CreateCounter<long>(
            name: "security.auth.successes",
            unit: "{successes}",
            description: "Number of successful authentications");

        _idorAttempts = meter.CreateCounter<long>(
            name: "security.idor.attempts",
            unit: "{attempts}",
            description: "Number of IDOR attempts detected");

        _attackPatterns = meter.CreateCounter<long>(
            name: "security.attacks.detected",
            unit: "{attacks}",
            description: "Number of attack patterns detected");

        _rateLimitHits = meter.CreateCounter<long>(
            name: "security.ratelimit.hits",
            unit: "{hits}",
            description: "Number of rate limit violations");

        _accountLockouts = meter.CreateCounter<long>(
            name: "security.account.lockouts",
            unit: "{lockouts}",
            description: "Number of account lockouts");
    }

    public void RecordAuthFailure(string? reason = null)
    {
        var tags = new TagList();
        if (!string.IsNullOrEmpty(reason))
        {
            tags.Add("reason", reason);
        }
        _authFailures.Add(1, tags);
    }

    public void RecordAuthSuccess(string? userRole = null)
    {
        var tags = new TagList();
        if (!string.IsNullOrEmpty(userRole))
        {
            tags.Add("role", userRole);
        }
        _authSuccesses.Add(1, tags);
    }

    public void RecordIdorAttempt(string resourceType)
    {
        var tags = new TagList
        {
            { "resource_type", resourceType }
        };
        _idorAttempts.Add(1, tags);
    }

    public void RecordAttackPattern(string attackType)
    {
        var tags = new TagList
        {
            { "attack_type", attackType }
        };
        _attackPatterns.Add(1, tags);
    }

    public void RecordRateLimitHit(string? endpoint = null)
    {
        var tags = new TagList();
        if (!string.IsNullOrEmpty(endpoint))
        {
            tags.Add("endpoint", endpoint);
        }
        _rateLimitHits.Add(1, tags);
    }

    public void RecordAccountLockout()
    {
        _accountLockouts.Add(1);
    }
}