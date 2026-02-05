using System.Text.RegularExpressions;
using EduCheck.Infrastructure.Security.Events;

namespace EduCheck.Infrastructure.Security;

/// <summary>
/// Detects common attack patterns in request data.
/// </summary>
public class AttackPatternDetector
{
    // SQL Injection patterns
    private static readonly Regex[] SqlInjectionPatterns = new[]
    {
        new Regex(@"('\s*(or|and)\s*'?\d*'?\s*=)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(union\s+select)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(;\s*(drop|delete|truncate|alter|create)\s)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(--\s*$)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(/\*.*\*/)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(exec\s*\()", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(xp_\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    // XSS patterns
    private static readonly Regex[] XssPatterns = new[]
    {
        new Regex(@"<script[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"javascript\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"on\w+\s*=", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"<iframe", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"<object", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"<embed", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"expression\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    // Path traversal patterns
    private static readonly Regex[] PathTraversalPatterns = new[]
    {
        new Regex(@"\.\.[\\/]", RegexOptions.Compiled),
        new Regex(@"%2e%2e[\\/]", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"%252e%252e[\\/]", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\.\.%2f", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"%00", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    /// <summary>
    /// Checks for SQL injection patterns.
    /// </summary>
    public SqlInjectionAttemptEvent? DetectSqlInjection(string? value, string? parameter)
    {
        if (string.IsNullOrEmpty(value)) return null;

        foreach (var pattern in SqlInjectionPatterns)
        {
            if (pattern.IsMatch(value))
            {
                return new SqlInjectionAttemptEvent
                {
                    Parameter = parameter,
                    DetectedPattern = pattern.ToString(),
                    SanitizedValue = SanitizeForLogging(value),
                    Blocked = true
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Checks for XSS patterns.
    /// </summary>
    public XssAttemptEvent? DetectXss(string? value, string? parameter)
    {
        if (string.IsNullOrEmpty(value)) return null;

        foreach (var pattern in XssPatterns)
        {
            if (pattern.IsMatch(value))
            {
                return new XssAttemptEvent
                {
                    Parameter = parameter,
                    DetectedPattern = pattern.ToString(),
                    Blocked = true
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Checks for path traversal patterns.
    /// </summary>
    public PathTraversalAttemptEvent? DetectPathTraversal(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        foreach (var pattern in PathTraversalPatterns)
        {
            if (pattern.IsMatch(value))
            {
                return new PathTraversalAttemptEvent
                {
                    AttemptedPath = SanitizeForLogging(value),
                    DetectedPattern = pattern.ToString(),
                    Blocked = true
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Sanitizes a value for safe logging.
    /// </summary>
    private static string SanitizeForLogging(string value)
    {
        if (value.Length > 200)
        {
            value = value.Substring(0, 200) + "...[truncated]";
        }
        return value.Replace("\r", "").Replace("\n", " ");
    }
}