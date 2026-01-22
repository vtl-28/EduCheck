using EduCheck.Application.DTOs.FraudReport;
using EduCheck.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduCheck.API.Controllers;

/// <summary>
/// API endpoints for managing fraud reports.
/// All endpoints require authentication and implement IDOR prevention.
/// </summary>
[ApiController]
[Route("api/fraud-reports")]
[Authorize]
public class FraudReportsController : ControllerBase
{
    private readonly IFraudReportService _fraudReportService;
    private readonly ILogger<FraudReportsController> _logger;

    public FraudReportsController(
        IFraudReportService fraudReportService,
        ILogger<FraudReportsController> logger)
    {
        _fraudReportService = fraudReportService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a new fraud report
    /// </summary>
    /// <param name="request">Report details</param>
    /// <returns>The created report</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateFraudReportResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CreateFraudReportResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(CreateFraudReportResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateReport([FromBody] CreateFraudReportRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("SECURITY: CreateReport - Unable to identify current user");
            return Unauthorized(new CreateFraudReportResponse
            {
                Success = false,
                Message = "User not authenticated",
                Errors = new List<string> { "Unable to identify current user" }
            });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new CreateFraudReportResponse
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            });
        }

        _logger.LogInformation("CreateReport request. UserId: {UserId}, InstituteName: {InstituteName}",
            userId, request.ReportedInstituteName);

        var result = await _fraudReportService.CreateReportAsync(userId.Value, request);

        if (!result.Success)
        {
            if (result.Message.Contains("limit", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, result);
            }
            return BadRequest(result);
        }

        return Created($"/api/fraud-reports/{result.Data!.Id}", result);
    }

    /// <summary>
    /// Get current user's submitted fraud reports
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 50)</param>
    /// <returns>Paginated list of reports</returns>
    [HttpGet]
    [ProducesResponseType(typeof(FraudReportsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserReports(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("SECURITY: GetUserReports - Unable to identify current user");
            return Unauthorized(new FraudReportsResponse
            {
                Success = false,
                Message = "User not authenticated",
                Errors = new List<string> { "Unable to identify current user" }
            });
        }

        _logger.LogInformation("GetUserReports request. UserId: {UserId}, Page: {Page}, PageSize: {PageSize}",
            userId, page, pageSize);

        var result = await _fraudReportService.GetUserReportsAsync(userId.Value, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific fraud report by ID
    /// </summary>
    /// <param name="id">Report ID</param>
    /// <returns>The report if found and owned by user</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FraudReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FraudReportResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(FraudReportResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetReportById([FromRoute] Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("SECURITY: GetReportById - Unable to identify current user");
            return Unauthorized(new FraudReportResponse
            {
                Success = false,
                Message = "User not authenticated",
                Errors = new List<string> { "Unable to identify current user" }
            });
        }

        if (id <= Guid.Empty)
        {
            return BadRequest(new FraudReportResponse
            {
                Success = false,
                Message = "Invalid report ID",
                Errors = new List<string> { "Report ID must be a positive number" }
            });
        }

        _logger.LogInformation("GetReportById request. UserId: {UserId}, ReportId: {ReportId}", userId, id);

        var result = await _fraudReportService.GetReportByIdAsync(userId.Value, id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Extracts user ID from JWT token claims.
    /// SECURITY: This is the ONLY place we get the user ID - never from request body/params.
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}