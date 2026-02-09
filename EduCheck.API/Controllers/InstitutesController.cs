using EduCheck.Application.DTOs.Institute;
using EduCheck.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduCheck.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InstitutesController : ControllerBase
{
    private readonly IInstituteService _instituteService;
    private readonly ILogger<InstitutesController> _logger;

    public InstitutesController(IInstituteService instituteService, ILogger<InstitutesController> logger)
    {
        _instituteService = instituteService;
        _logger = logger;
    }

    /// <summary>
    /// Search institutes by name or accreditation number
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(InstituteSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(InstituteSearchResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] string? province = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new InstituteSearchResponse
            {
                Success = false,
                Message = "Search query is required",
                Errors = new List<string> { "Please provide a search query" }
            });
        }

        if (query.Length < 2)
        {
            return BadRequest(new InstituteSearchResponse
            {
                Success = false,
                Message = "Search query too short",
                Errors = new List<string> { "Search query must be at least 2 characters" }
            });
        }

        if (query.Length > 255)
        {
            return BadRequest(new InstituteSearchResponse
            {
                Success = false,
                Message = "Search query too long",
                Errors = new List<string> { "Search query cannot exceed 255 characters" }
            });
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var request = new InstituteSearchRequest
        {
            Query = query,
            Province = province,
            Page = page,
            PageSize = pageSize
        };

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = null;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        var result = await _instituteService.SearchInstitutesAsync(request, userId);

        return Ok(result);
    }

    /// <summary>
    /// Get institute details by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstituteDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(InstituteDetailResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new InstituteDetailResponse
            {
                Success = false,
                Message = "Invalid institute ID",
                Errors = new List<string> { "Institute ID must be a positive number" }
            });
        }

        var result = await _instituteService.GetInstituteByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}