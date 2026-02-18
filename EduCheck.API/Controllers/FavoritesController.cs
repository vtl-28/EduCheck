using EduCheck.Application.DTOs.Favorites;
using EduCheck.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace EduCheck.API.Controllers;

/// <summary>
/// API endpoints for managing user's favorite institutes.
/// All endpoints require authentication and implement IDOR prevention.
/// </summary>
[ApiController]
[Route("api/favorites")]
[Authorize]
[EnableRateLimiting("favorites")]
public class FavoritesController : ControllerBase
{
    private readonly IFavoritesService _favoritesService;
    private readonly ILogger<FavoritesController> _logger;

    public FavoritesController(
        IFavoritesService favoritesService,
        ILogger<FavoritesController> logger)
    {
        _favoritesService = favoritesService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's favorite institutes
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 50)</param>
    /// <returns>Paginated list of favorites sorted by most recently added</returns>
    [HttpGet]
    [ProducesResponseType(typeof(FavoritesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetFavorites(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("SECURITY: GetFavorites - Unable to identify current user");
            return Unauthorized(new FavoritesResponse
            {
                Success = false,
                Message = "User not authenticated",
                Errors = new List<string> { "Unable to identify current user" }
            });
        }

        _logger.LogInformation("GetFavorites request. UserId: {UserId}, Page: {Page}, PageSize: {PageSize}",
            userId, page, pageSize);

        var result = await _favoritesService.GetUserFavoritesAsync(userId.Value, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Add an institute to favorites
    /// </summary>
    /// <param name="instituteId">Institute ID to add</param>
    /// <returns>The created favorite entry</returns>
    [HttpPost("{instituteId:int}")]
    [ProducesResponseType(typeof(AddFavoriteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AddFavoriteResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(AddFavoriteResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(AddFavoriteResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> AddFavorite([FromRoute] int instituteId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("SECURITY: AddFavorite - Unable to identify current user");
            return Unauthorized(new AddFavoriteResponse
            {
                Success = false,
                Message = "User not authenticated",
                Errors = new List<string> { "Unable to identify current user" }
            });
        }

        if (instituteId <= 0)
        {
            return BadRequest(new AddFavoriteResponse
            {
                Success = false,
                Message = "Invalid institute ID",
                Errors = new List<string> { "Institute ID must be a positive number" }
            });
        }

        _logger.LogInformation("AddFavorite request. UserId: {UserId}, InstituteId: {InstituteId}",
            userId, instituteId);

        var result = await _favoritesService.AddFavoriteAsync(userId.Value, instituteId);

        if (!result.Success)
        {
            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(result);
            }
            if (result.Message.Contains("already", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(result);
            }
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetFavoriteStatus), new { instituteId }, result);
    }

    /// <summary>
    /// Remove an institute from favorites
    /// </summary>
    /// <param name="instituteId">Institute ID to remove</param>
    /// <returns>Success message</returns>
    [HttpDelete("{instituteId:int}")]
    [ProducesResponseType(typeof(RemoveFavoriteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RemoveFavoriteResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RemoveFavoriteResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RemoveFavorite([FromRoute] int instituteId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("SECURITY: RemoveFavorite - Unable to identify current user");
            return Unauthorized(new RemoveFavoriteResponse
            {
                Success = false,
                Message = "User not authenticated",
                Errors = new List<string> { "Unable to identify current user" }
            });
        }

        if (instituteId <= 0)
        {
            return BadRequest(new RemoveFavoriteResponse
            {
                Success = false,
                Message = "Invalid institute ID",
                Errors = new List<string> { "Institute ID must be a positive number" }
            });
        }

        _logger.LogInformation("RemoveFavorite request. UserId: {UserId}, InstituteId: {InstituteId}",
            userId, instituteId);

        var result = await _favoritesService.RemoveFavoriteAsync(userId.Value, instituteId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Check if an institute is in the user's favorites
    /// </summary>
    /// <param name="instituteId">Institute ID to check</param>
    /// <returns>Favorite status with timestamp if favorited</returns>
    [HttpGet("{instituteId:int}/status")]
    [ProducesResponseType(typeof(FavoriteStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FavoriteStatusResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetFavoriteStatus([FromRoute] int instituteId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            _logger.LogWarning("SECURITY: GetFavoriteStatus - Unable to identify current user");
            return Unauthorized(new FavoriteStatusResponse
            {
                Success = false,
                Message = "User not authenticated",
                Errors = new List<string> { "Unable to identify current user" }
            });
        }

        if (instituteId <= 0)
        {
            return BadRequest(new FavoriteStatusResponse
            {
                Success = false,
                Message = "Invalid institute ID",
                Errors = new List<string> { "Institute ID must be a positive number" }
            });
        }

        var result = await _favoritesService.GetFavoriteStatusAsync(userId.Value, instituteId);
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