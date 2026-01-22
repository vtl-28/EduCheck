using EduCheck.Domain.Enums;

namespace EduCheck.Application.DTOs.Admin;

/// <summary>
/// DTO for displaying a fraud report to admins.
/// Includes additional fields not shown to students.
/// </summary>
public class AdminFraudReportDto
{
    /// <summary>
    /// Unique identifier for the report.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the reported institute.
    /// </summary>
    public string ReportedInstituteName { get; set; } = string.Empty;

    /// <summary>
    /// Address of the reported institute.
    /// </summary>
    public string? ReportedInstituteAddress { get; set; }

    /// <summary>
    /// Phone number of the reported institute.
    /// </summary>
    public string? ReportedInstitutePhone { get; set; }

    /// <summary>
    /// Description of the fraudulent activity.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the report.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Severity level of the report.
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// When the report was submitted.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the report was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Reporter information (null if anonymous).
    /// </summary>
    public ReporterDto? Reporter { get; set; }
}

/// <summary>
/// DTO for reporter (student) information.
/// </summary>
public class ReporterDto
{
    public Guid StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for filtering fraud reports.
/// </summary>
public class AdminFraudReportFilterRequest
{
    /// <summary>
    /// Filter by status.
    /// </summary>
    public FraudReportStatus? Status { get; set; }

    /// <summary>
    /// Filter by severity.
    /// </summary>
    public FraudSeverity? Severity { get; set; }

    /// <summary>
    /// Filter by start date (inclusive).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter by end date (inclusive).
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Filter by province (from address).
    /// </summary>
    public string? Province { get; set; }

    /// <summary>
    /// Filter by city (from address).
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Search term for institute name.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Page number (default: 1).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Items per page (default: 20, max: 100).
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// DTO for fraud report statistics.
/// </summary>
public class FraudReportStatisticsDto
{
    public int TotalReports { get; set; }
    public int SubmittedCount { get; set; }
    public int UnderReviewCount { get; set; }
    public int VerifiedCount { get; set; }
    public int DismissedCount { get; set; }
    public int ActionTakenCount { get; set; }
    public int ReportsToday { get; set; }
    public int ReportsThisWeek { get; set; }
    public int ReportsThisMonth { get; set; }
}

/// <summary>
/// Pagination DTO for admin fraud reports list.
/// </summary>
public class AdminPaginationDto
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}