using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EduCheck.API.Metrics;

/// <summary>
/// Custom business metrics for the EduCheck API.
/// </summary>
public class BusinessMetrics
{
    private readonly Counter<long> _instituteSearches;
    private readonly Counter<long> _userRegistrations;
    private readonly Counter<long> _fraudReports;
    private readonly Counter<long> _loginAttempts;
    private readonly Counter<long> _favoritesActions;
    private readonly Histogram<double> _searchDuration;

    public BusinessMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("educheck-api");

        // Institute searches
        _instituteSearches = meter.CreateCounter<long>(
            name: "educheck.institute.searches",
            unit: "{searches}",
            description: "Number of institute searches");

        // User registrations
        _userRegistrations = meter.CreateCounter<long>(
            name: "educheck.user.registrations",
            unit: "{registrations}",
            description: "Number of user registrations");

        // Fraud reports
        _fraudReports = meter.CreateCounter<long>(
            name: "educheck.fraud.reports",
            unit: "{reports}",
            description: "Number of fraud reports submitted");

        // Login attempts
        _loginAttempts = meter.CreateCounter<long>(
            name: "educheck.auth.login_attempts",
            unit: "{attempts}",
            description: "Number of login attempts");

        // Favorites actions
        _favoritesActions = meter.CreateCounter<long>(
            name: "educheck.favorites.actions",
            unit: "{actions}",
            description: "Number of favorites add/remove actions");

        // Search duration
        _searchDuration = meter.CreateHistogram<double>(
            name: "educheck.institute.search_duration",
            unit: "ms",
            description: "Duration of institute searches in milliseconds");
    }

    /// <summary>
    /// Records an institute search.
    /// </summary>
    public void RecordInstituteSearch(bool found, string? searchType = null)
    {
        var tags = new TagList
        {
            { "found", found.ToString().ToLower() },
            { "search_type", searchType ?? "unknown" }
        };
        _instituteSearches.Add(1, tags);
    }

    /// <summary>
    /// Records the duration of an institute search.
    /// </summary>
    public void RecordSearchDuration(double durationMs, bool found)
    {
        var tags = new TagList
        {
            { "found", found.ToString().ToLower() }
        };
        _searchDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records a user registration.
    /// </summary>
    public void RecordUserRegistration(string userType)
    {
        var tags = new TagList
        {
            { "user_type", userType }
        };
        _userRegistrations.Add(1, tags);
    }

    /// <summary>
    /// Records a fraud report submission.
    /// </summary>
    public void RecordFraudReport()
    {
        _fraudReports.Add(1);
    }

    /// <summary>
    /// Records a login attempt.
    /// </summary>
    public void RecordLoginAttempt(bool success, string? failureReason = null)
    {
        var tags = new TagList
        {
            { "success", success.ToString().ToLower() }
        };

        if (!success && !string.IsNullOrEmpty(failureReason))
        {
            tags.Add("failure_reason", failureReason);
        }

        _loginAttempts.Add(1, tags);
    }

    /// <summary>
    /// Records a favorites action.
    /// </summary>
    public void RecordFavoritesAction(string action)
    {
        var tags = new TagList
        {
            { "action", action }
        };
        _favoritesActions.Add(1, tags);
    }
}