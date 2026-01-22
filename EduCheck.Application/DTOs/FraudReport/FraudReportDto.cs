using System.ComponentModel.DataAnnotations;

namespace EduCheck.Application.DTOs.FraudReport;

/// <summary>
/// DTO for displaying a fraud report.
/// </summary>
public class FraudReportDto
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
    /// When the report was submitted.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO for submitting a new fraud report.
/// </summary>
public class CreateFraudReportRequest
{
    /// <summary>
    /// Name of the institute being reported.
    /// </summary>
    [Required(ErrorMessage = "Institute name is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Institute name must be between 2 and 255 characters")]
    public string ReportedInstituteName { get; set; } = string.Empty;

    /// <summary>
    /// Address of the institute being reported.
    /// </summary>
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string? ReportedInstituteAddress { get; set; }

    /// <summary>
    /// Phone number of the institute being reported.
    /// </summary>
    [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters")]
    [RegularExpression(@"^[0-9\s\-\+\(\)]+$", ErrorMessage = "Invalid phone number format")]
    public string? ReportedInstitutePhone { get; set; }

    /// <summary>
    /// Description of why this institute is being reported.
    /// </summary>
    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000, MinimumLength = 20, ErrorMessage = "Description must be between 20 and 2000 characters")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Pagination DTO for fraud reports list.
/// </summary>
public class FraudReportPaginationDto
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}