using EduCheck.Application.DTOs.Admin;
using EduCheck.Application.Interfaces;
using EduCheck.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCheck.API.Controllers;

/// <summary>
/// Admin API endpoints for managing fraud reports.
/// All endpoints require Admin role.
/// </summary>
[ApiController]
[Route("api/admin/fraud-reports")]
[Authorize(Roles = "Admin")]
public class AdminFraudReportsController : ControllerBase
{
    private readonly IAdminFraudReportService _adminFraudReportService;
    private readonly ILogger<AdminFraudReportsController> _logger;

    public AdminFraudReportsController(
        IAdminFraudReportService adminFraudReportService,
        ILogger<AdminFraudReportsController> logger)
    {
        _adminFraudReportService = adminFraudReportService;
        _logger = logger;
    }

    /// <summary>
    /// Get all fraud reports with optional filtering
    /// </summary>
    /// <param name="status">Filter by status</param>
    /// <param name="severity">Filter by severity</param>
    /// <param name="fromDate">Filter by start date</param>
    /// <param name="toDate">Filter by end date</param>
    /// <param name="province">Filter by province</param>
    /// <param name="city">Filter by city</param>
    /// <param name="searchTerm">Search by institute name</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <returns>Paginated list of fraud reports</returns>
    [HttpGet]
    [ProducesResponseType(typeof(AdminFraudReportsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllReports(
        [FromQuery] FraudReportStatus? status = null,
        [FromQuery] FraudSeverity? severity = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? province = null,
        [FromQuery] string? city = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation(
            "Admin GetAllReports request. Status: {Status}, Severity: {Severity}, Page: {Page}",
            status, severity, page);

        var filter = new AdminFraudReportFilterRequest
        {
            Status = status,
            Severity = severity,
            FromDate = fromDate,
            ToDate = toDate,
            Province = province,
            City = city,
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize
        };

        var result = await _adminFraudReportService.GetAllReportsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific fraud report by ID
    /// </summary>
    /// <param name="id">Report ID</param>
    /// <returns>The fraud report details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AdminFraudReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdminFraudReportResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReportById([FromRoute] Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new AdminFraudReportResponse
            {
                Success = false,
                Message = "Invalid report ID",
                Errors = new List<string> { "Report ID cannot be empty" }
            });
        }

        _logger.LogInformation("Admin GetReportById request. ReportId: {ReportId}", id);

        var result = await _adminFraudReportService.GetReportByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get fraud report statistics
    /// </summary>
    /// <returns>Statistics summary</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(FraudReportStatisticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatistics()
    {
        _logger.LogInformation("Admin GetStatistics request");

        var result = await _adminFraudReportService.GetStatisticsAsync();
        return Ok(result);
    }
}