using EduCheck.Application.DTOs.FraudReport;

namespace EduCheck.Application.Interfaces;

/// <summary>
/// Service interface for managing fraud reports.
/// All methods require the userId to be extracted from JWT token (IDOR prevention).
/// </summary>
public interface IFraudReportService
{
    /// <summary>
    /// Submits a new fraud report.
    /// </summary>
    /// <param name="userId">User ID from JWT token</param>
    /// <param name="request">Report details</param>
    /// <returns>The created report</returns>
    Task<CreateFraudReportResponse> CreateReportAsync(Guid userId, CreateFraudReportRequest request);

    /// <summary>
    /// Gets the current student's submitted reports.
    /// </summary>
    /// <param name="userId">User ID from JWT token</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paginated list of reports</returns>
    Task<FraudReportsResponse> GetUserReportsAsync(Guid userId, int page = 1, int pageSize = 10);

    /// <summary>
    /// Gets a specific report by ID (only if owned by the user).
    /// </summary>
    /// <param name="userId">User ID from JWT token</param>
    /// <param name="reportId">Report ID</param>
    /// <returns>The report if found and owned by user</returns>
    Task<FraudReportResponse> GetReportByIdAsync(Guid userId, Guid reportId);
}