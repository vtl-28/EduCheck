using EduCheck.Application.DTOs.Admin;

namespace EduCheck.Application.Interfaces;

/// <summary>
/// Service interface for admin fraud report management.
/// </summary>
public interface IAdminFraudReportService
{
    /// <summary>
    /// Gets all fraud reports with optional filtering.
    /// </summary>
    Task<AdminFraudReportsResponse> GetAllReportsAsync(AdminFraudReportFilterRequest filter);

    /// <summary>
    /// Gets a specific fraud report by ID.
    /// </summary>
    Task<AdminFraudReportResponse> GetReportByIdAsync(Guid reportId);

    /// <summary>
    /// Gets fraud report statistics.
    /// </summary>
    Task<FraudReportStatisticsResponse> GetStatisticsAsync();
}