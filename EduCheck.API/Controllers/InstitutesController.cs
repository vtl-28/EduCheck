using EduCheck.Application.DTOs;
using EduCheck.Application.DTOs.Institute;
using EduCheck.Application.Interfaces;
using EduCheck.Infrastructure.Data;
using EduCheck.Infrastructure.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduCheck.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InstitutesController : ControllerBase
{
    private readonly IInstituteService _instituteService;
    private readonly INearbyInstituteService _nearbyInstituteService;
    private readonly ILogger<InstitutesController> _logger;

    public InstitutesController(IInstituteService instituteService, INearbyInstituteService nearbyInstituteService, ILogger<InstitutesController> logger)
    {
        _instituteService = instituteService;
        _nearbyInstituteService = nearbyInstituteService;
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

    /// <summary>
    /// Get institutes within a specified radius of a location (with pagination)
    /// </summary>
    /// <param name="lat">Latitude of user's location</param>
    /// <param name="lng">Longitude of user's location</param>
    /// <param name="radius">Search radius in kilometers (default: 10km, max: 100km)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 50)</param>
    /// <returns>Paginated list of nearby institutes sorted by distance</returns>
    [HttpGet("nearby")]
    public async Task<ActionResult<PaginatedResponse<NearbyInstituteDto>>> GetNearbyInstitutes(
        [FromQuery] decimal lat,
        [FromQuery] decimal lng,
        [FromQuery] decimal radius = 10m,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Validation
        if (!DistanceCalculator.IsValidLatitude(lat))
            return BadRequest(new { error = $"Invalid latitude: {lat}" });

        if (!DistanceCalculator.IsValidLongitude(lng))
            return BadRequest(new { error = $"Invalid longitude: {lng}" });

        if (radius <= 0 || radius > 100)
            return BadRequest(new { error = $"Invalid radius: {radius}" });

        if (page < 1)
            return BadRequest(new { error = $"Invalid page: {page}" });

        if (pageSize < 1 || pageSize > 50)
            return BadRequest(new { error = $"Invalid pageSize: {pageSize}" });

        try
        {
            var result = await _nearbyInstituteService.GetNearbyAsync(
                lat, lng, radius, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting nearby institutes");
            return StatusCode(500, new { error = "Failed to get nearby institutes" });
        }
    }

}