using EduCheck.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduCheck.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IGeocodingService geocodingService,
        ILogger<AdminController> logger)
    {
        _geocodingService = geocodingService;
        _logger = logger;
    }

    /// <summary>
    /// Geocode all institutes that have addresses but no coordinates
    /// This is a one-time operation and may take several minutes
    /// </summary>
    /// <returns>Number of institutes successfully geocoded</returns>
    [HttpPost("geocode-institutes")]
    public async Task<ActionResult<GeocodeResponse>> GeocodeAllInstitutes()
    {
        _logger.LogInformation("Admin triggered geocoding of all institutes");

        try
        {
            var successCount = await _geocodingService.GeocodeAllInstitutesAsync();

            return Ok(new GeocodeResponse
            {
                Success = true,
                Message = $"Successfully geocoded {successCount} institutes",
                InstitutesGeocoded = successCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during geocoding process");
            return StatusCode(500, new GeocodeResponse
            {
                Success = false,
                Message = $"Geocoding failed: {ex.Message}",
                InstitutesGeocoded = 0
            });
        }
    }
}

public class GeocodeResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int InstitutesGeocoded { get; set; }
}