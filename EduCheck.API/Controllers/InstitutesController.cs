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
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InstitutesController> _logger;

    public InstitutesController(IInstituteService instituteService, ApplicationDbContext context, ILogger<InstitutesController> logger)
    {
        _instituteService = instituteService;
        _context = context;
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
        _logger.LogInformation(
            "Searching for institutes near ({Lat}, {Lng}) within {Radius}km (page {Page}, size {PageSize})",
            lat, lng, radius, page, pageSize);


        if (!DistanceCalculator.IsValidLatitude(lat))
        {
            return BadRequest(new { error = $"Invalid latitude: {lat}. Must be between -90 and +90" });
        }

        if (!DistanceCalculator.IsValidLongitude(lng))
        {
            return BadRequest(new { error = $"Invalid longitude: {lng}. Must be between -180 and +180" });
        }

        if (radius <= 0 || radius > 100)
        {
            return BadRequest(new { error = $"Invalid radius: {radius}. Must be between 0 and 100 km" });
        }

        if (page < 1)
        {
            return BadRequest(new { error = $"Invalid page: {page}. Must be >= 1" });
        }

        if (pageSize < 1 || pageSize > 50)
        {
            return BadRequest(new { error = $"Invalid pageSize: {pageSize}. Must be between 1 and 50" });
        }


        var institutesWithLocation = await _context.Institutes
            .Where(i => i.Latitude != null && i.Longitude != null && i.IsActive)
            .ToListAsync();

        _logger.LogInformation(
            "Found {Count} institutes with coordinates",
            institutesWithLocation.Count);


        var nearbyInstitutes = institutesWithLocation
            .Select(institute => new
            {
                Institute = institute,
                Distance = DistanceCalculator.CalculateDistance(
                    lat, lng,
                    institute.Latitude!.Value,
                    institute.Longitude!.Value)
            })
            .Where(x => x.Distance <= radius)
            .OrderBy(x => x.Distance)
            .ToList();

        var totalCount = nearbyInstitutes.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        _logger.LogInformation(
            "Found {Count} institutes within {Radius}km",
            totalCount, radius);


        var paginatedData = nearbyInstitutes
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NearbyInstituteDto
            {
                Id = x.Institute.Id,
                InstitutionName = x.Institute.InstitutionName,
                ProviderType = x.Institute.ProviderType,
                PhysicalAddress = x.Institute.PhysicalAddress,
                City = x.Institute.City,
                Province = x.Institute.Province,
                Latitude = x.Institute.Latitude!.Value,
                Longitude = x.Institute.Longitude!.Value,
                Distance = Math.Round(x.Distance, 1),
                IsAccredited = x.Institute.IsAccredited
            })
            .ToList();

        var response = new PaginatedResponse<NearbyInstituteDto>
        {
            Data = paginatedData,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };

        return Ok(response);
    }

}